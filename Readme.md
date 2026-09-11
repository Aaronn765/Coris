# Coris

Application de suivi DSI — incidents et projets.

README technique de référence. Ce document décrit le code présent dans le dépôt et sert de point d’orientation pour toute modification. Il ne décrit pas un processus utilisateur ni un déploiement de production complet.

## 1. Vue d’ensemble

Le dépôt contient une application web en deux processus :

- backend/ : API ASP.NET Core 10, logique métier, accès SQL Server par Entity Framework Core, import Excel et export Excel.
- frontend/ : application Next.js 16 / React 19 / TypeScript, consommant l’API HTTP.
- backend/tests/ : tests d’intégration xUnit de l’API et tests de l’import Excel sur SQLite en mémoire.
- tests/robot/ : tests de contrat HTTP et tests UI Robot Framework contre des processus déjà démarrés.

Flux nominal :

~~~
Navigateur
   │ fetch JSON / téléchargement XLSX
   ▼
frontend/src/services/api.ts
   ▼
backend/Controllers
   ▼
backend/Services
   ▼
backend/Data/AppDbContext.cs ── Entity Framework Core ── SQL Server
~~~

Le backend est la source de vérité pour les validations métier, les filtres, le tri, la pagination, les numéros, les agrégats et la persistance. Le frontend porte les validations d’ergonomie, l’état des écrans et les conversions de saisie, mais ses contrôles ne remplacent jamais ceux de l’API.

### Caractéristiques actuelles

- Base cible : SQL Server, base IncidentsDSI.
- Développement local : API sur http://localhost:5000, frontend sur http://localhost:3000.
- Swagger activé par défaut.
- Migrations appliquées automatiquement au démarrage si Database:ApplyMigrations=true.
- Import Excel au démarrage désactivé par défaut.
- Aucun mécanisme d’authentification ou d’autorisation n’est présent dans le code.
- Aucune route DELETE n’existe. La suppression fonctionnelle d’un référentiel se fait par désactivation (actif=false). Les incidents et projets ne sont pas supprimables via l’API.
- Les dates sont échangées en JSON comme des dates ISO et sont généralement construites en UTC par le frontend.

### Stack et versions déclarées

| Zone | Technologies principales |
|---|---|
| backend | ASP.NET Core / .NET 10, Entity Framework Core 10.0.2, SQL Server provider 10.0.2, ClosedXML 0.104.2, Swashbuckle 6.6.2 |
| frontend | Next.js 16.3.3, React 19.2.8, TypeScript, Tailwind CSS 4, shadcn/Base UI, TanStack Table, React Hook Form, Zod, Lucide |
| tests | xUnit + ASP.NET Core MVC Testing + SQLite côté backend ; Robot Framework, Requests et Browser côté tests système |

## 2. Démarrage local

### Prérequis

- .NET SDK compatible avec net10.0.
- Node.js et npm.
- SQL Server local ou SQL Server Express accessible par localhost\SQLEXPRESS.
- Python si les tests Robot sont nécessaires.

### Configuration backend

La configuration par défaut est dans backend/appsettings.json :

~~~json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=IncidentsDSI;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  },
  "Cors": { "AllowedOrigins": ["http://localhost:3000"] },
  "Database": { "ApplyMigrations": true },
  "Data": { "ImportExcelOnStartup": false, "ExcelPath": "" },
  "Swagger": { "Enabled": true }
}
~~~

appsettings.Development.json ne remplace que les niveaux de logs. Properties/launchSettings.json déclare les URLs http://localhost:5000 et https://localhost:5001 et l’environnement Development.

Lancer l’API depuis la racine :

~~~powershell
dotnet run --project backend/IncidentsDsi.Api.csproj --urls http://localhost:5000
~~~

Adresses :

- frontend : http://localhost:3000
- API : http://localhost:5000/api
- Swagger : http://localhost:5000/swagger

### Configuration frontend

Le frontend lit NEXT_PUBLIC_API_URL dans frontend/src/services/api.ts. La valeur par défaut est http://localhost:5000/api.

Exemple dans frontend/.env.local (fichier local ignoré par Git) :

~~~env
NEXT_PUBLIC_API_URL=http://localhost:5000/api
~~~

Lancer le frontend :

~~~powershell
Set-Location frontend
npm install
npm run dev
~~~

Commandes frontend disponibles :

~~~powershell
npm run dev
npm run build
npm run start
npm run lint
~~~

NEXT_PUBLIC_API_URL est une variable publique Next.js : elle ne doit jamais contenir un secret.

## 3. Déploiement sur IIS

La procédure de publication de l’API ASP.NET Core et du frontend Next.js derrière IIS se trouve dans [deploy/iis/README.md](deploy/iis/README.md). Le script [deploy/iis/publish.ps1](deploy/iis/publish.ps1) prépare les deux dossiers publiables.

## 4. Architecture backend

### Initialisation de l’application

backend/Program.cs :

1. configure les contrôleurs et la sérialisation JSON en camelCase, avec omission des valeurs null ;
2. active Swagger ;
3. enregistre AppDbContext avec SQL Server et les services applicatifs ;
4. configure CORS à partir de Cors:AllowedOrigins ;
5. installe ExceptionHandlingMiddleware ;
6. désactive la redirection HTTPS uniquement dans l’environnement Testing ;
7. mappe les contrôleurs ;
8. applique les migrations, exécute éventuellement les imports et normalise les statuts de projets démarrés.

Services enregistrés :

| Interface | Implémentation | Responsabilité |
|---|---|---|
| IIncidentService | IncidentService | CRUD incident, filtres, tri, pagination, durée, export des données |
| IReferentielService | ReferentielService | lecture, création, renommage et activation des référentiels |
| IDashboardService | DashboardService | agrégats incidents et évolution mensuelle |
| IProjetService | ProjetService | CRUD projet, responsables, étapes, filtres et normalisation des statuts |
| IProjetDashboardService | ProjetDashboardService | agrégats du portefeuille projets |
| IExcelImportService | ExcelImportService | import conditionnel des incidents et projets historiques |

### Gestion des erreurs

backend/Common/ExceptionHandlingMiddleware.cs convertit les exceptions non traitées par les contrôleurs en application/problem+json :

| Exception | HTTP | Usage |
|---|---:|---|
| ValidationException applicative | 400 | règle métier ou référentiel invalide |
| KeyNotFoundException | 404 | ressource absente si levée par un service |
| DbUpdateException | 409 | conflit ou échec d’écriture en base |
| autre Exception | 500 | erreur interne, message générique côté client |

Les erreurs de validation automatique de ApiController produisent également un problème HTTP 400, avec le détail des champs dans errors. backend/Common/ApiExceptions.cs contient l’exception applicative dédiée aux validations métier.

## 5. Contrat HTTP de l’API

Tous les chemins ci-dessous sont préfixés par /api. Les propriétés JSON sont en camelCase.

### Incidents

Contrôleur : backend/Controllers/IncidentsController.cs. Règles et requêtes : backend/Services/IncidentService.cs. DTO : backend/DTOs/IncidentDtos.cs.

| Méthode | Route | Réponse | Fonction |
|---|---|---|---|
| GET | /incidents | PagedResult&lt;IncidentDto&gt; | liste filtrée, triée et paginée |
| GET | /incidents/{id} | IncidentDto ou 404 | détail avec tous les référentiels liés |
| POST | /incidents | 201 + IncidentDto | création et génération du numéro |
| PUT | /incidents/{id} | 200 + IncidentDto ou 404 | remplacement des champs éditables, numéro conservé |
| GET | /incidents/export | fichier .xlsx | export de tous les résultats filtrés, sans pagination |

#### Payload d’écriture incident

IncidentWriteDto est utilisé par POST et PUT.

| Champ | Type | Contraintes |
|---|---|---|
| dateDeclaration | date | obligatoire, non nulle |
| dateFin | date nullable | >= dateDeclaration si renseignée |
| typeIncidentId | entier | référentiel actif obligatoire |
| applicationId | entier | référentiel actif obligatoire |
| intitule | chaîne | 3 à 300 caractères |
| description | chaîne | 10 à 10 000 caractères |
| entiteId | entier | référentiel actif obligatoire |
| criticiteId | entier | référentiel actif obligatoire |
| impact, cause, actionsMenees, solution, actionsEnCours, mesuresPreventives | chaînes nullables | maximum 10 000 caractères ; les blancs sont convertis en null |
| risqueId | entier | référentiel actif obligatoire |
| responsableN1Id | entier | responsable actif obligatoire |
| responsableN2Id | entier nullable | responsable actif si présent |
| statutId | entier | statut incident actif obligatoire |

Le backend supprime les espaces autour des chaînes obligatoires et optionnelles. Il ne permet pas de référencer un élément inactif, même si son identifiant existe.

#### Numéro et durée

- À la création, le numéro est INC-{année de dateDeclaration}-{séquence sur 4 chiffres} : INC-2026-0001.
- La séquence est stockée par année dans IncidentNumberSequences et incrémentée dans une transaction Serializable.
- Une modification de dateDeclaration ne renumérote pas un incident existant.
- dureeMinutes est calculée en mémoire : durée en minutes, arrondie à l’entier inférieur, jamais négative.
- dureeJours est calculée en mémoire : durée en jours, arrondie à l’entier supérieur, jamais négative.
- Si dateFin est absente, les deux durées sont null.

#### Paramètres de liste incidents

Tous sont des query parameters de GET /incidents et sont aussi acceptés par /incidents/export.

| Paramètre | Type / défaut | Sémantique |
|---|---|---|
| search | chaîne | recherche LIKE sur numéro, intitulé, description, application, entité, responsables, cause, solution et actions menées |
| dateDebut | date | date de déclaration minimale, incluse |
| dateFin | date | date de déclaration maximale, incluse jusqu’à la fin du jour |
| statutId, typeIncidentId, applicationId, entiteId, criticiteId, risqueId | entier nullable | filtre exact par identifiant |
| responsableId | entier nullable | correspond à ResponsableN1Id ou ResponsableN2Id |
| dureeMinMinutes | entier nullable | exige une date de fin et une durée au moins égale à la borne |
| dureeMaxMinutes | entier nullable | borne maximale incluse ; la valeur est normalisée en entier positif |
| page | entier, 1 | minimum 1 |
| pageSize | entier, 20 | borné entre 1 et 100 |
| sortBy | dateDeclaration | valeurs prévues : numero, dateDeclaration, dateFin, duree, intitule, application, typeIncident, criticite, statut, createdAt |
| sortDirection | desc | seul asc force l’ordre croissant ; toute autre valeur devient desc |

Les dates de début supérieures aux dates de fin et les bornes de durée inversées sont rejetées en 400. Le tri duree utilise DATEDIFF sur SQL Server. Avec un fournisseur non SQL Server, il charge les incidents filtrés en mémoire avant de trier et paginer ; c’est le chemin utilisé par les tests SQLite.

PagedResult&lt;T&gt; contient items, page, pageSize, totalCount et totalPages.

### Tableaux de bord

| Méthode | Route | Paramètres | Réponse |
|---|---|---|---|
| GET | /dashboard | dateDebut, dateFin optionnels | DashboardStatsDto incidents |
| GET | /dashboard/projets | aucun | ProjectDashboardStatsDto projets |

/dashboard filtre sur DateDeclaration, puis renvoie : total, en cours, clôturés, répartitions par criticité/application/type et évolution par année/mois. Les statuts considérés comme clôturés sont les variantes textuelles reconnues dans DashboardService.IsClosed ; IncidentsEnCours = total - incidents clôturés.

Le tableau de bord projets renvoie total, compteurs par statut (En cours, Planifié, Clôturé), taux moyen, répartition par domaine et répartition par statut. Les statuts personnalisés sont visibles dans la répartition mais ne sont comptés dans les compteurs nommés que s’ils correspondent à ces libellés après normalisation.

### Projets

Contrôleur : backend/Controllers/ProjetsController.cs. Règles : backend/Services/ProjetService.cs. DTO : backend/DTOs/ProjetDtos.cs.

| Méthode | Route | Réponse | Fonction |
|---|---|---|---|
| GET | /projets | PagedResult&lt;ProjetDto&gt; | liste filtrée, triée et paginée |
| GET | /projets/{id} | ProjetDto ou 404 | détail, responsables et étapes |
| POST | /projets | 201 + ProjetDto | création, numéro et enfants |
| PUT | /projets/{id} | 200 + ProjetDto ou 404 | remplacement du projet et de tous ses enfants |

#### Payload d’écriture projet

ProjetWriteDto :

| Champ | Type / contrainte |
|---|---|
| domaineProjetId | entier, domaine actif |
| nom | chaîne de 2 à 300 caractères |
| description | chaîne nullable, maximum 10 000 |
| tauxAvancement | décimal entre 0 et 1 inclus |
| dateDebut, dateFin, dateEcheance | dates nullables ; dateFin et dateEcheance ne peuvent précéder dateDebut |
| statutProjetId | entier, statut projet actif |
| support, acteursMetiers | chaînes nullables, maximum 5 000 |
| contraintes, commentaires | chaînes nullables, maximum 10 000 |
| responsableIds | liste d’identifiants de responsables actifs ; doublons dédupliqués |
| etapes | liste de EtapeProjetWriteDto |

Chaque étape contient nom (1 à 10 000), tauxAvancement (0 à 1), statutEtapeId actif, dates optionnelles, support (5 000), contraintes et commentaires (10 000), et ordre.

Points importants :

- id dans EtapeProjetWriteDto est accepté par le DTO mais n’est pas utilisé par le service.
- Lors d’un PUT, les liens ProjetResponsables et les EtapesProjet existants sont supprimés puis recréés à partir du payload. Une mise à jour partielle d’une étape n’existe pas.
- L’ordre réel est l’ordre du tableau etapes reçu ; la valeur ordre reçue est ignorée et le service affecte 0, 1, 2, ...
- Le numéro est PRJ-{année}-{séquence sur 4 chiffres}. L’année est celle de dateDebut, sinon de dateEcheance, sinon l’année UTC courante. Le numéro est conservé lors d’un update.
- Un projet demandé comme Planifié est automatiquement basculé en En cours s’il est déjà démarré : taux global positif, date de début passée, ou étape démarrée par son taux, sa date ou son statut.
- La normalisation est appliquée à la création et à la mise à jour, puis aux projets existants au démarrage via NormalizeStartedStatusesAsync.

#### Paramètres de liste projets

| Paramètre | Type / défaut | Sémantique |
|---|---|---|
| search | chaîne | recherche sur numéro, nom, description, domaine, support, acteurs métiers, contraintes, commentaires, responsables et noms d’étapes |
| domaineProjetId, statutProjetId, responsableId | entier nullable | filtres exacts ; le responsable cherche dans la relation multiple |
| dateEcheanceApres | date | échéance minimale incluse |
| dateEcheanceAvant | date | échéance maximale incluse jusqu’à la fin du jour |
| tauxMin, tauxMax | décimal nullable | bornes normalisées entre 0 et 1 |
| page | entier, 1 | minimum 1 |
| pageSize | entier, 20 | borné entre 1 et 100 |
| sortBy | dateEcheance | numero, nom, domaine, statut, taux, dateEcheance, createdAt |
| sortDirection | asc | seul desc force l’ordre décroissant ; toute autre valeur devient asc |

### Référentiels

Contrôleur : backend/Controllers/ReferentielsController.cs. Service : backend/Services/ReferentielService.cs. Modèles : backend/Models/Referentiel.cs.

| Méthode | Route | Fonction |
|---|---|---|
| GET | /referentiels | renvoie tous les référentiels dans un seul ReferentielSetDto |
| GET | /referentiels/{type} | renvoie un référentiel |
| GET | /referentiels/{type}/{id} | renvoie une valeur ou 404 |
| POST | /referentiels/{type} | crée une valeur, 201 |
| PUT | /referentiels/{type}/{id} | renomme et/ou active/désactive, 200 ou 404 |

includeInactive=false par défaut. Les listes sont triées par Nom. La création et la mise à jour refusent un nom vide ou un doublon de nom dans le même référentiel. Il n’existe pas de suppression physique.

Types canoniques : responsables, applications, types-incidents, entites, criticites, risques, statuts, domaines-projet, statuts-projet, statuts-etape.

Alias reconnus par le backend :

- responsables : responsable, responsables ;
- applications : application, applications ;
- types incidents : typeincident, typesincident, types-incidents, type-incidents ;
- entités : entite, entites ;
- criticités : criticite, criticites ;
- risques : risque, risques ;
- statuts incidents : statut, statuts ;
- domaines projets : domaineprojet, domainesprojet, domaines-projet ;
- statuts projets : statutprojet, statutsprojet, statuts-projet ;
- statuts étapes : statutetape, statutsetape, statuts-etape.

## 6. Modèle de données et migrations

### Modèle relationnel

~~~
Incidents
 ├─ TypeIncident, Application, Entite, Criticite, Risque, Statut
 ├─ ResponsableN1 ─┐
 └─ ResponsableN2 ─┘

Projets
 ├─ DomaineProjet, StatutProjet
 ├─< ProjetResponsables >─ Responsable
 └─< EtapesProjet >─ StatutEtape

IncidentNumberSequences : clé Year → LastValue
ProjetNumberSequences   : clé Year → LastValue
~~~

### Fichiers qui définissent le schéma

backend/Data/AppDbContext.cs est la définition lisible du modèle : DbSets, noms de tables, clés, tailles, index, relations et données initiales. Les relations vers les référentiels utilisent Restrict. Les enfants d’un projet (ProjetResponsables, EtapesProjet) utilisent Cascade quand le projet est supprimé en base.

Tables métier :

| Table | Rôle |
|---|---|
| Incidents | enregistrement incident et liens vers les référentiels |
| Projets | projet, progression, dates, statut et informations de suivi |
| EtapesProjet | étapes ordonnées d’un projet |
| ProjetResponsables | relation plusieurs-à-plusieurs projet/responsable |
| IncidentNumberSequences, ProjetNumberSequences | séquences de numérotation par année |
| Responsables, Applications, TypesIncident, Entites, Criticites, Risques, Statuts | référentiels incidents |
| DomainesProjet, StatutsProjet, StatutsEtape | référentiels projets |

Index importants :

- nom sur chaque référentiel ;
- Incidents.Numero et Projets.Numero uniques ;
- Incidents.DateDeclaration et (DateDeclaration, Id) ;
- (référentielId, DateDeclaration) pour les filtres incidents ;
- Projets.DateEcheance, (StatutProjetId, DateEcheance), (DomaineProjetId, DateEcheance) ;
- (EtapesProjet.ProjetId, Ordre) ;
- clé composite (ProjetId, ResponsableId) sur ProjetResponsables.

### Données initiales

SeedReferentiels dans AppDbContext.cs est la source de lecture des valeurs initiales :

| Référentiel | Valeurs initiales remarquables |
|---|---|
| responsables | 14 responsables historiques, dont ARSENE, LEGRE, MATHURIN, BCEAO |
| applications | 7 valeurs, dont ABM, AMPLITUDE, CARTHAGO, SMARTACCESS, SWIFT, VNEURON |
| types incidents | Infrastructure / électrique, application, réseaux |
| entités | 20 périmètres/agences |
| criticités | Fort, Moyenne |
| risques | Fort, Moyen |
| statuts incidents | En cours, Clôturé |
| domaines projets | 11 domaines, dont MONETIQUE, SECURITE, APPLICATIONS, REPORTING |
| statuts projets | Planifié, En cours, Clôturé, Suspendu |
| statuts étapes | Non démarrée, Démarrée, Terminée |

Modifier le seed ne modifie pas directement une base déjà migrée : il faut générer une nouvelle migration afin que les données HasData soient synchronisées.

### Historique des migrations

Les fichiers *.Designer.cs et AppDbContextModelSnapshot.cs sont générés par EF Core ; ils ne doivent pas être édités manuellement.

1. 20260826122238_InitialCreate : schéma incidents, référentiels incidents et seed initial.
2. 20260826125835_FixIncidentNumberSequenceYear : corrige IncidentNumberSequences.Year pour qu’il soit une clé métier non auto-incrémentée.
3. 20260826144009_AddIncidentListIndexes : index de liste et de tri incidents avec DateDeclaration.
4. 20260826144038_AddIncidentReferenceFilterIndexes : index composites pour entité, responsables et risque.
5. 20260826164728_AddProjects : tables projets, étapes, relation responsables, séquences projets et référentiels projets.

backend/Migrations/initial.sql est un script SQL historique couvrant le schéma incidents initial et la correction de séquence. Il ne représente pas le schéma actuel complet avec les projets. Pour une base normale, utiliser les migrations EF Core via Database:ApplyMigrations ou les commandes dotnet ef.

Le jeu de données de démonstration actuellement utilisé par l’application est disponible dans [backend/Database/IncidentsDSI-demo-data.sql](backend/Database/IncidentsDSI-demo-data.sql). Sa procédure de restauration est détaillée dans [backend/Database/README.md](backend/Database/README.md).

Commandes EF utiles depuis backend/ :

~~~powershell
dotnet ef migrations list --project IncidentsDsi.Api.csproj
dotnet ef migrations add NomDeLaMigration --project IncidentsDsi.Api.csproj
dotnet ef database update --project IncidentsDsi.Api.csproj
~~~

La factory backend/Data/DesignTimeDbContextFactory.cs permet à dotnet ef de construire le contexte à partir de appsettings.json, appsettings.Development.json et des variables d’environnement.

## 7. Import et export Excel

### Import au démarrage

L’import n’a pas de route HTTP. Il est déclenché uniquement par backend/Program.cs si Data:ImportExcelOnStartup=true, après l’application des migrations.

Résolution de Data:ExcelPath :

1. chemin absolu utilisé tel quel ;
2. chemin relatif résolu depuis le content root du backend ;
3. si le fichier n’existe pas, recherche du premier .xlsx dans le content root puis son dossier parent.

Les deux méthodes sont ensuite appelées :

- ImportIfEmptyAsync ne travaille que si Incidents est vide et lit la première feuille du classeur ;
- ImportProjectsIfEmptyAsync ne travaille que si Projets est vide et cherche la feuille PROJETS INFORMATIQUES sans tenir compte des accents et de la casse.

Un fichier déjà partiellement chargé n’est pas complété : la présence d’une seule ligne dans la table suffit à empêcher l’import correspondant.

### Import incidents

Fichier : backend/Services/ExcelImportService.cs. Les colonnes sont indexées à partir de 1 :

| Colonne | Champ |
|---:|---|
| 2 | date de déclaration |
| 3 | date de fin |
| 5 | type incident |
| 6 | application |
| 7 | intitulé |
| 8 | description |
| 9 | entité |
| 10 | criticité |
| 11 | impact |
| 12 | cause |
| 13 | risque |
| 14 | actions menées |
| 15 | solution |
| 16 | actions en cours |
| 17 | responsable N1 |
| 18 | responsable N2 optionnel |
| 19 | mesures préventives |
| 20 | statut |

La première ligne est considérée comme l’en-tête. Une date de déclaration absente ignore la ligne. Un référentiel obligatoire inconnu ignore la ligne et écrit un avertissement dans les logs. Les correspondances de noms sont insensibles à la casse et aux accents. Les chaînes sont nettoyées et tronquées aux longueurs applicatives. Les numéros importés recommencent à 1 par année dans un contexte vide.

### Import projets

La feuille projet est interprétée par blocs : à partir de la ligne 4, chaque ligne dont la colonne 3 est renseignée démarre un projet ; les lignes suivantes jusqu’au prochain nom sont ses étapes.

| Colonne | Projet | Étape |
|---:|---|---|
| 2 | domaine | — |
| 3 | nom | — |
| 4 | taux global | — |
| 5 | responsables séparés par /, \\, ; ou retour ligne | — |
| 6 | description | — |
| 7 | date d’échéance | — |
| 8 | — | nom |
| 9 | — | taux |
| 10 | statut racine si aucun bloc d’étape exploitable | statut |
| 11 | — | date de début |
| 12 | — | date de fin |
| 13 | support | support |
| 15 | contraintes | contraintes |
| 16 | commentaires | commentaires |

Un domaine ou un responsable absent des référentiels est créé automatiquement. Le domaine vide devient Non classé. Le taux global manquant est la moyenne des taux d’étapes. Le statut projet est déduit des statuts d’étapes : toutes terminées → Clôturé, au moins une démarrée → En cours, sinon Planifié.

### Export incidents

GET /api/incidents/export construit le classeur en mémoire avec ClosedXML. Il réutilise les filtres et le tri de IncidentService, mais exporte l’ensemble du résultat filtré et ignore page/pageSize. Le contrôleur IncidentsController.cs définit les 22 colonnes, fige la première ligne, ajuste les largeurs et renvoie un nom incidents-yyyyMMddHHmmss.xlsx.

## 8. Architecture frontend

Le frontend utilise l’App Router Next.js. Toutes les pages fonctionnelles sont des Client Components car elles chargent des données et gèrent des états locaux.

### Routes applicatives

| URL | Fichier | Fonction |
|---|---|---|
| / | src/app/page.tsx | tableau de bord incidents/projets, récents et graphiques SVG |
| /incidents | src/app/incidents/page.tsx | liste serveur, recherche, filtres, tri, pagination et export Excel |
| /incidents/nouveau | src/app/incidents/nouveau/page.tsx | wrapper de création incident |
| /incidents/:id | src/app/incidents/[id]/page.tsx | détail incident et formulaire de modification |
| /projets | src/app/projets/page.tsx | cartes projets, recherche, filtres, pagination, édition rapide des étapes |
| /projets/nouveau | src/app/projets/nouveau/page.tsx | wrapper de création projet |
| /projets/:id | src/app/projets/[id]/page.tsx | détail projet, progression et formulaire complet de modification |
| /administration | src/app/administration/page.tsx | onglets de gestion des dix référentiels |

### Client API et types

frontend/src/services/api.ts est l’unique couche HTTP :

- construit les query strings ;
- envoie Content-Type: application/json pour les écritures ;
- force cache: no-store ;
- lit detail/title des problèmes API ;
- met en cache la promesse de /referentiels séparément pour les listes actives et toutes les listes ;
- vide ce cache après une création ou une mise à jour de référentiel ;
- expose les méthodes incidents, dashboard, projets et référentiels.

frontend/src/types/index.ts contient les types de données consommés par les composants. Les types détaillés (IncidentWithDetails, ProjetWithDetails) correspondent aux DTO backend enrichis avec les objets référentiels.

### Écrans et flux frontend

- src/app/page.tsx charge au montage quatre ressources en parallèle : dashboard incidents, cinq incidents récents, dashboard projets et cinq projets récents. La sélection d’une année relance le dashboard incidents avec dateDebut/dateFin. Le graphique mensuel complète les mois absents avec zéro ; les classements type/applicatif affichent au maximum huit valeurs. Le bouton Exporter sérialise le SVG courant côté navigateur, sans appel API.
- src/app/incidents/page.tsx charge les référentiels actifs puis la page incidents. La recherche est débouncée de 320 ms. La pagination et le tri sont serveur ; TanStack Table est configuré en mode manuel. L’export transmet l’état courant des filtres à /incidents/export.
- src/app/incidents/columns.tsx définit le rendu et les identifiants de tri des colonnes. Les tris imbriqués sont traduits par sortMap dans incidents/page.tsx (application.nom → application, etc.).
- src/components/incidents/data-table.tsx est une table générique réutilisable, avec modes local et serveur. Dans l’application actuelle, la liste incidents utilise le mode serveur.
- src/components/incidents/IncidentForm.tsx charge les référentiels actifs, valide côté client avec Zod/React Hook Form, convertit les dates HTML en ISO et supprime responsableN2Id si la valeur vaut 0. En création, le statut par défaut est l’identifiant 1.
- src/app/incidents/[id]/page.tsx charge un incident, affiche son résumé et réutilise IncidentForm en mode update.
- src/app/projets/page.tsx charge les référentiels projets actifs, recherche après 280 ms et liste 12 cartes par page. L’édition rapide des étapes reconstruit un payload projet complet et recalcule le taux global comme moyenne des taux d’étapes.
- src/components/projects/ProjectForm.tsx est le formulaire complet projet : domaine, statut, progression en pourcentage dans l’UI puis conversion en [0,1], responsables multiples, étapes dynamiques, dates et textes de suivi.
- src/app/projets/[id]/page.tsx affiche la progression et les étapes, puis réutilise ProjectForm pour la modification complète.
- src/components/administration/ReferentielManager.tsx charge toutes les valeurs (includeInactive=true), crée, renomme et bascule actif. Les modifications sont des appels PUT ; elles ne suppriment pas les lignes.

### Présentation et composants partagés

- src/app/globals.css : Tailwind v4, variables de thème, couleurs Coris, badges, gradients, animations et réduction de mouvement.
- src/app/layout.tsx : métadonnées, polices Geist et enveloppe AppLayout.
- src/components/layout/AppLayout.tsx : navigation desktop/mobile et détection de route active. Ajouter une entrée de menu nécessite de modifier le tableau navigation de ce fichier.
- src/components/ui/*.tsx : primitives UI générées/adaptées au style shadcn/Base UI (Button, Card, Dialog, Input, Label, Select, Table, Tabs, Textarea). Elles ne contiennent pas de logique métier.
- src/lib/formatters.ts : affichage des durées et dates en français.
- src/lib/utils.ts : fusion des classes CSS avec clsx et tailwind-merge.
- src/validations/incident.schema.ts : validation Zod du formulaire incident. Il n’existe pas de schéma Zod équivalent pour les projets.

## 9. Carte complète des fichiers

Les dossiers bin/, obj/, .next/, node_modules/, tests/robot/results/ et les fichiers de build sont des sorties générées et ne sont pas des points d’extension. Les fichiers Excel locaux sont ignorés par Git et servent de données d’import/export ou de fixtures locales.

### Racine

| Fichier | Utilité |
|---|---|
| Readme.md | cette cartographie technique |
| .gitignore | ignore les builds, environnements locaux, logs, rapports Robot et fichiers Excel |
| logo.png | copie de logo à la racine, non référencée directement par le frontend actuel |
| Déclaration_Incidents_DSI_CBI_CI_2026.xlsx | classeur historique d’incidents local ; utilisé comme fixture potentielle d’import/tests |
| incidents-export.xlsx | export Excel local ; fichier de donnée, pas du code source |
| SUIVI PROJETS DSI 25-08-2025.xlsx | classeur historique projets avec feuille PROJETS INFORMATIQUES |

### Backend

| Fichier | Utilité |
|---|---|
| IncidentsDsi.Api.csproj | cible net10.0, dépendances EF Core SQL Server/Design, ClosedXML et Swagger |
| Program.cs | composition de l’application, DI, middleware, CORS, migrations et import au démarrage |
| appsettings.json | configuration locale par défaut |
| appsettings.Development.json | niveaux de logs en développement |
| Properties/launchSettings.json | profils et URLs de lancement dotnet |
| Common/ApiExceptions.cs | exception de validation métier |
| Common/ExceptionHandlingMiddleware.cs | traduction centralisée des exceptions en problèmes HTTP |
| Controllers/IncidentsController.cs | routes incidents et génération du fichier Excel exporté |
| Controllers/DashboardController.cs | route dashboard incidents |
| Controllers/ProjetDashboardController.cs | route dashboard projets |
| Controllers/ProjetsController.cs | routes projets |
| Controllers/ReferentielsController.cs | routes génériques des référentiels |
| Data/AppDbContext.cs | DbSets, mapping relationnel, index et seed |
| Data/DesignTimeDbContextFactory.cs | construction du contexte pour les commandes EF Core |
| Models/Incident.cs | entité incident et propriétés calculées de durée |
| Models/IncidentNumberSequence.cs | séquence incidents par année |
| Models/Projet.cs | entités projet, étape, relation responsables, référentiels projet et séquence projet |
| Models/Referentiel.cs | base commune et sept référentiels incidents |
| DTOs/CommonDtos.cs | enveloppe générique paginée |
| DTOs/IncidentDtos.cs | payloads, réponses et paramètres incidents |
| DTOs/ProjetDtos.cs | payloads, réponses, étapes et paramètres projets |
| DTOs/DashboardDtos.cs | réponses des agrégats incidents |
| DTOs/ReferentielDtos.cs | réponses et payloads référentiels |
| Services/IIncidentService.cs | contrat de service incidents |
| Services/IncidentService.cs | implémentation incidents, règles, filtres, tri et mapping DTO |
| Services/IProjetService.cs | contrat projet |
| Services/ProjetService.cs | implémentation projets, enfants et statuts démarrés |
| Services/IReferentielService.cs | contrat référentiels |
| Services/ReferentielService.cs | implémentation générique des référentiels et alias de type |
| Services/IDashboardService.cs | contrat dashboard incidents |
| Services/DashboardService.cs | requêtes d’agrégation incidents |
| Services/IProjetDashboardService.cs | contrat dashboard projets |
| ProjetDashboardService | implémentation du dashboard projets, co-localisée en fin de Services/ProjetService.cs |
| Services/IExcelImportService.cs | contrat des deux imports Excel |
| Services/ExcelImportService.cs | lecture ClosedXML, correspondance référentiels et création des entités importées |

### Backend — migrations

| Fichier | Utilité |
|---|---|
| Migrations/20260826122238_InitialCreate.cs | migration initiale incidents/référentiels |
| Migrations/20260826122238_InitialCreate.Designer.cs | modèle généré associé |
| Migrations/20260826125835_FixIncidentNumberSequenceYear.cs | correction de la séquence par année |
| Migrations/20260826125835_FixIncidentNumberSequenceYear.Designer.cs | modèle généré associé |
| Migrations/20260826144009_AddIncidentListIndexes.cs | index d’optimisation des listes incidents |
| Migrations/20260826144009_AddIncidentListIndexes.Designer.cs | modèle généré associé |
| Migrations/20260826144038_AddIncidentReferenceFilterIndexes.cs | index des filtres par références |
| Migrations/20260826144038_AddIncidentReferenceFilterIndexes.Designer.cs | modèle généré associé |
| Migrations/20260826164728_AddProjects.cs | schéma, index et seed projets |
| Migrations/20260826164728_AddProjects.Designer.cs | modèle généré associé |
| Migrations/AppDbContextModelSnapshot.cs | snapshot courant généré par EF Core |
| Migrations/initial.sql | export SQL historique partiel, non canonique pour l’état actuel |

### Backend — tests

| Fichier | Utilité |
|---|---|
| tests/IncidentsDsi.Api.Tests.csproj | projet xUnit et dépendances de test |
| tests/ApiWebApplicationFactory.cs | remplace SQL Server par SQLite en mémoire et désactive migrations/import/Swagger pour les tests |
| tests/ApiTests.cs | contrat d’API incidents, référentiels, dashboard, export et validations |
| tests/ProjetTests.cs | création projet, relation multiple, étapes, dashboard et normalisation d’un projet démarré |
| tests/ExcelImportTests.cs | import historique incidents et projets depuis les classeurs Excel accessibles dans les dossiers parents |

### Frontend — configuration et assets

| Fichier | Utilité |
|---|---|
| frontend/package.json | scripts npm et dépendances |
| frontend/package-lock.json | verrouillage des versions npm |
| frontend/tsconfig.json | TypeScript strict, alias @/* vers src/*, mode bundler |
| frontend/next.config.ts | configuration Next.js, dont la sortie standalone pour IIS |
| frontend/eslint.config.mjs | ESLint Next Core Web Vitals + TypeScript et exclusions de build |
| frontend/postcss.config.mjs | plugin PostCSS Tailwind |
| frontend/components.json | configuration shadcn, alias et feuille CSS principale |
| frontend/README.md | documentation frontend historique, moins complète que ce README racine |
| frontend/.env.example | exemple de NEXT_PUBLIC_API_URL |
| frontend/.gitignore | exclusions propres à Next/npm et fichiers d’environnement |
| frontend/public/logo.png | logo effectivement utilisé par AppLayout via /logo.png |
| frontend/public/file.svg, globe.svg, next.svg, vercel.svg, window.svg | assets de template, non utilisés par les écrans actuels |
| frontend/src/app/favicon.ico | favicon |

### Frontend — code source

| Fichier | Utilité |
|---|---|
| src/app/layout.tsx | layout racine, métadonnées et polices |
| src/app/globals.css | thème, classes visuelles et responsive global |
| src/app/page.tsx | dashboard et composants SVG de graphiques |
| src/app/incidents/page.tsx | liste incidents et état des query parameters |
| src/app/incidents/columns.tsx | définition des colonnes et liens du tableau incidents |
| src/app/incidents/nouveau/page.tsx | route de création incident |
| src/app/incidents/[id]/page.tsx | route détail/édition incident |
| src/app/projets/page.tsx | liste cartes projets et édition rapide des étapes |
| src/app/projets/nouveau/page.tsx | route de création projet |
| src/app/projets/[id]/page.tsx | route détail/édition projet |
| src/app/administration/page.tsx | onglets des référentiels |
| src/components/layout/AppLayout.tsx | navigation et structure desktop/mobile |
| src/components/incidents/IncidentForm.tsx | formulaire incident création/update |
| src/components/incidents/data-table.tsx | table générique TanStack, pagination/tri manuel ou local |
| src/components/projects/ProjectForm.tsx | formulaire projet, responsables et étapes |
| src/components/administration/ReferentielManager.tsx | CRUD logique et activation des référentiels côté UI |
| src/components/ui/button.tsx | primitive bouton et variantes |
| src/components/ui/card.tsx | primitives carte |
| src/components/ui/dialog.tsx | primitives modal |
| src/components/ui/input.tsx | primitive input |
| src/components/ui/label.tsx | primitive label |
| src/components/ui/select.tsx | primitive select avec Base UI |
| src/components/ui/table.tsx | primitives table |
| src/components/ui/tabs.tsx | primitives onglets |
| src/components/ui/textarea.tsx | primitive textarea |
| src/services/api.ts | toutes les requêtes HTTP et types de payload/query frontend |
| src/types/index.ts | modèles TypeScript de l’API |
| src/validations/incident.schema.ts | validation client incident |
| src/lib/formatters.ts | formatage date/durée |
| src/lib/utils.ts | fusion des classes CSS |

### Tests Robot

| Fichier | Utilité |
|---|---|
| tests/robot/README.md | commandes et prérequis rapides des suites Robot |
| tests/robot/requirements.txt | Robot Framework et plugin HTTP |
| tests/robot/requirements-ui.txt | plugin navigateur Robot |
| tests/robot/resources/api_keywords.resource | sessions HTTP, payloads de test et mots-clés partagés |
| tests/robot/api_contract.robot | contrat API nominal : référentiels, incidents, filtres, tri, dashboard, export, CORS et validations |
| tests/robot/bug_probes.robot | régressions ciblées : dates, intégrité des références, dashboard, durée et tri |
| tests/robot/data_seed.robot | création de données de test via API, avec tokens RF-... |
| tests/robot/ui_smoke.robot | smoke UI dashboard, exports SVG, responsive, liste, formulaire et administration |
| tests/robot/results/ | rapports générés, ignorés par Git ; ne pas modifier à la main |

## 10. Guide de modification par besoin

Cette section est la carte d’intervention principale.

| Besoin | Fichiers à modifier en priorité | Vérifications à ajouter/adapter |
|---|---|---|
| Ajouter un champ incident | Models/Incident.cs, Data/AppDbContext.cs, DTOs/IncidentDtos.cs, Services/IncidentService.cs, frontend/src/types/index.ts, frontend/src/services/api.ts, IncidentForm.tsx | colonne/validation dans IncidentsController.cs pour l’export, affichage dans columns.tsx ou [id]/page.tsx, tests ApiTests.cs et Robot si contrat public |
| Modifier la validation d’un incident | DTOs/IncidentDtos.cs pour les annotations et règles de payload, Services/IncidentService.cs pour les règles métier, frontend/src/validations/incident.schema.ts et IncidentForm.tsx pour l’ergonomie | test 400 backend + smoke formulaire si le message ou le parcours change |
| Ajouter/modifier un filtre incident | DTOs/IncidentDtos.cs (IncidentQueryParameters), Services/IncidentService.cs (ApplyFilters), frontend/src/services/api.ts, src/app/incidents/page.tsx | index correspondant dans AppDbContext.cs puis migration si nécessaire, contrat API Robot |
| Ajouter une option de tri incident | IncidentService.ApplyOrdering, IncidentQuery frontend, état sorting et sortMap dans incidents/page.tsx, éventuellement columns.tsx | test asc/desc, index si le volume le justifie |
| Modifier le numéro incident | IncidentService.CreateAsync, IncidentNumberSequence, AppDbContext.cs, migration si le stockage change | tests de séquence dans ApiTests.cs et api_contract.robot |
| Modifier le calcul de durée | propriétés calculées de Models/Incident.cs, tri/filtre durée dans IncidentService.cs, formatters.ts pour l’affichage | tests bornes, incident ouvert et date de fin égale |
| Ajouter un indicateur dashboard incident | DTOs/DashboardDtos.cs, DashboardService.cs, éventuellement DashboardController.cs si paramètres nouveaux, puis frontend/src/services/api.ts et src/app/page.tsx | tests d’agrégation backend et UI si le sélecteur/graphique change |
| Modifier la définition d’un statut clôturé | DashboardService.IsClosed, classes de badges dans page.tsx, columns.tsx et pages détail | tests dashboard et rendu si le libellé métier change |
| Ajouter un champ projet | Models/Projet.cs, AppDbContext.cs, ProjetDtos.cs, ProjetService.cs, frontend/src/types/index.ts, ProjetWritePayload dans api.ts, ProjectForm.tsx | affichage dans projets/page.tsx et [id]/page.tsx, import Excel si le champ vient du classeur, migration + tests |
| Modifier les étapes projet | ProjetDtos.cs, ProjetService.AddResponsibilitiesAndSteps, ProjectForm.tsx, projets/page.tsx, éventuellement [id]/page.tsx | garder en tête que le PUT remplace toutes les étapes ; tests ProjetTests.cs |
| Modifier la règle Planifié → En cours | ProjetService.NormalizeStatusIdAsync, HasStartedAsync, HasStarted, NormalizeStartedStatusesAsync | tester création, update et normalisation au redémarrage |
| Modifier les compteurs dashboard projets | ProjetService.cs dans ProjetDashboardService, DTO ProjectDashboardStatsDto, types/API frontend, ProjectDashboardView dans app/page.tsx | tests de statuts et moyenne |
| Ajouter une catégorie de référentiel | Models/Referentiel.cs (nouvelle classe), AppDbContext.cs (DbSet, mapping, seed), ReferentielDtos.cs, ReferentielService.cs (switch + alias), api.ts (type, chemin, getter), ReferentielManager.tsx, administration/page.tsx | migration et test lecture/création/désactivation |
| Modifier une valeur initiale de référentiel | AppDbContext.SeedReferentiels | générer une migration EF ; ne pas modifier seulement initial.sql |
| Changer la désactivation référentiel | ReferentielService, ReferentielManager.tsx, règles EnsureReferencesExistAsync des services incidents/projets | vérifier qu’un élément inactif reste visible avec includeInactive=true mais refusé à l’écriture |
| Modifier les colonnes ou le format export Excel | IncidentsController.Export | test de type MIME, nom, contenu XLSX et ordre des colonnes |
| Modifier les colonnes de l’import incidents | ExcelImportService.ImportIfEmptyAsync, ExcelImportTests.cs, classeur fixture | vérifier la ligne d’en-tête, les indices de colonnes et les référentiels inconnus |
| Modifier l’import projets | ExcelImportService.ImportProjectsIfEmptyAsync, helpers de statut/responsables et ExcelImportTests.cs | vérifier les blocs, la feuille exacte, les étapes et la numérotation |
| Ajouter une route frontend | créer src/app/ROUTE/page.tsx, brancher api.ts, puis ajouter AppLayout.navigation si elle doit être accessible au menu | smoke Robot UI et gestion des états loading/error |
| Modifier la navigation globale | src/components/layout/AppLayout.tsx | tester desktop et barre mobile |
| Modifier le thème ou les badges | src/app/globals.css, puis les fonctions statusClass/criticiteClass des pages concernées | npm run lint et smoke UI responsive |
| Ajouter/modifier un composant UI partagé | fichier correspondant dans src/components/ui/, puis les consommateurs | vérifier que les composants métier restent sans styles dupliqués |
| Changer l’URL ou le contrat réseau | frontend/.env.example, frontend/src/services/api.ts, backend/appsettings.json, éventuellement Program.cs pour CORS | vérifier CORS, API et tests Robot contre les nouvelles URLs |
| Modifier le schéma SQL ou un index | d’abord Data/AppDbContext.cs, ensuite nouvelle migration dans backend/Migrations/ | dotnet ef migrations list, dotnet test, mise à jour contrôlée de la base |
| Modifier le contrat HTTP | contrôleur + DTO/service backend, frontend/src/services/api.ts + types, suites xUnit/Robot | conserver les codes 200/201/400/404/409 documentés ci-dessus |

## 11. Tests et validation

### Tests backend

Depuis la racine :

~~~powershell
dotnet test backend/tests/IncidentsDsi.Api.Tests.csproj
~~~

Les tests xUnit démarrent l’API avec WebApplicationFactory&lt;Program&gt;, remplacent SQL Server par SQLite en mémoire et appellent EnsureCreated. Les migrations ne sont donc pas testées par ces tests ; une modification de migration doit aussi être vérifiée avec une vraie base SQL Server ou dotnet ef database update.

Les tests d’import recherchent les classeurs .xlsx dans les dossiers parents du répertoire de sortie. Pour des tests reproductibles, vérifier que les fichiers historiques nécessaires sont présents et non ambigus.

### Build et lint frontend

~~~powershell
Set-Location frontend
npm run lint
npm run build
~~~

### Tests Robot

Installation :

~~~powershell
py -m pip install -r tests/robot/requirements.txt
py -m pip install -r tests/robot/requirements-ui.txt
rfbrowser init
~~~

Prérequis d’exécution :

- API disponible sur http://localhost:5000/api ;
- frontend disponible sur http://localhost:3000 ;
- base de test isolée recommandée, car les suites HTTP créent des données persistantes avec des tokens RF-....

Commandes :

~~~powershell
py -m robot --outputdir tests/robot/results/api tests/robot/api_contract.robot
py -m robot --outputdir tests/robot/results/bugs tests/robot/bug_probes.robot
py -m robot --outputdir tests/robot/results/data tests/robot/data_seed.robot
py -m robot --outputdir tests/robot/results/ui tests/robot/ui_smoke.robot
py -m robot --outputdir tests/robot/results tests/robot
~~~

Les suites Robot vérifient notamment les alias de référentiels, le tri alphabétique, les codes HTTP, les validations de dates et longueurs, les numéros/durées, la recherche multi-champs, la pagination, le dashboard, l’export XLSX, CORS, la navigation UI et les exports SVG du dashboard.

## 12. Règles de maintenance

1. Pour une règle métier, modifier d’abord le service backend puis aligner le frontend et les tests.
2. Pour une donnée persistée, modifier AppDbContext/les modèles et générer une migration ; ne pas éditer un fichier *.Designer.cs ou le snapshot à la main.
3. Pour un champ visible dans plusieurs écrans, rechercher tous les consommateurs du DTO dans frontend/src avant de modifier un seul écran.
4. Pour un référentiel, conserver la désactivation logique : les lignes référencées ne doivent pas être supprimées physiquement.
5. Pour les projets, traiter le payload comme un agrégat complet : un update remplace les responsables et les étapes.
6. Après chaque changement de contrat, exécuter au minimum le test backend concerné et npm run build si le frontend est touché.
7. Ne pas considérer backend/Migrations/initial.sql, les fichiers Excel, bin/, obj/, .next/ ou les rapports Robot comme la source de vérité du code courant.
