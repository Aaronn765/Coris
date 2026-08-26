# Application de suivi des incidents et projets DSI

Application web pour declarer, suivre et analyser les incidents DSI ainsi que les projets et leurs étapes.

## Fonctionnalites

- Tableau de bord avec les compteurs principaux, les derniers incidents et les repartitions par type/applicatif.
- Histogramme des incidents par mois, filtrable par annee, avec export SVG.
- Liste des incidents avec recherche, filtres, tri et pagination.
- Creation, consultation, modification et suppression des incidents.
- Administration des referentiels: statuts, criticites, applications, types, entites, risques et responsables.
- Export Excel des incidents.
- Import Excel historique disponible, mais desactive par defaut pour eviter de recharger le fichier au demarrage.
- Module Projets avec recherche, filtres, création, modification et suivi des étapes.
- Tableau de bord à deux onglets : Incidents et Projets.
- Référentiels projets : domaines, statuts projets et statuts étapes.

## Architecture

- `backend/`: API ASP.NET Core avec Entity Framework Core.
- `frontend/`: interface Next.js/React.
- `tests/robot/`: tests Robot Framework pour l'API, l'interface et l'ajout de donnees.
- Base de donnees cible: SQL Server, base `IncidentsDSI`.

## Demarrage local

Prerequis:

- .NET SDK
- Node.js
- SQL Server Express ou SQL Server local
- Python avec Robot Framework pour les tests Robot

Lancer le backend:

```powershell
dotnet run --project backend/IncidentsDsi.Api.csproj --urls http://localhost:5000
```

Lancer le frontend:

```powershell
cd frontend
npm install
npm run dev
```

Adresses locales:

- Site: `http://localhost:3000`
- API: `http://localhost:5000/api`
- Swagger: `http://localhost:5000/swagger`

## Base de donnees

La connexion locale par defaut est configuree pour SQL Server Express:

```text
Server=localhost\SQLEXPRESS;Database=IncidentsDSI;Trusted_Connection=True;TrustServerCertificate=True;
```

Les migrations Entity Framework peuvent etre appliquees automatiquement au demarrage si `Database:ApplyMigrations` vaut `true`.

L'import Excel n'est pas lance a chaque demarrage. Le parametre important est:

```json
"Data": {
  "ImportExcelOnStartup": false
}
```

Pour importer un fichier une seule fois, renseigner `Data:ExcelPath`, passer `ImportExcelOnStartup` a `true`, demarrer l'API, puis remettre la valeur a `false`.

Le fichier `SUIVI PROJETS DSI 25-08-2025.xlsx` est reconnu automatiquement lorsqu'il est utilisé comme `Data:ExcelPath` : la feuille `PROJETS INFORMATIQUES` est importée dans `Projets`, `EtapesProjet` et `ProjetResponsables`. Les vues Excel « en cours », « planifiés » et « clôturés » sont représentées par le statut du projet, pas par des tables séparées.

Tables ajoutées : `Projets`, `ProjetNumberSequences`, `DomainesProjet`, `StatutsProjet`, `StatutsEtape`, `ProjetResponsables` et `EtapesProjet`.

## Tests

Tests backend:

```powershell
dotnet test backend/tests/IncidentsDsi.Api.Tests.csproj
```

Build frontend:

```powershell
cd frontend
npm run build
```

Tests Robot principaux:

```powershell
py -m robot --outputdir tests/robot/results/api tests/robot/api_contract.robot
py -m robot --outputdir tests/robot/results/ui tests/robot/ui_smoke.robot
py -m robot --outputdir tests/robot/results/data tests/robot/data_seed.robot
```

Les tests Robot supposent que l'API est disponible sur `http://localhost:5000/api` et que le site est disponible sur `http://localhost:3000`.
