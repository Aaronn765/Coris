# Guide humain de déploiement de Coris sur IIS

Ce guide explique comment installer les composants nécessaires, préparer SQL Server, publier l'application et la rendre accessible avec IIS.

Il s'adresse à une personne qui administre un serveur Windows, même si elle ne connaît pas encore cette application.

## 1. Ce qui sera installé

L'application comporte deux parties :

- une API ASP.NET Core 10, publiée dans `C:\Sites\Coris\api` ;
- un frontend Next.js, publié dans `C:\Sites\Coris\frontend`.

IIS reçoit les requêtes des utilisateurs. Il les transmet ensuite aux deux processus locaux :

```text
Navigateur
    |
    | HTTPS : 443 (ou HTTP : 8080 pour un test local)
    v
IIS - site Coris
    |-- /api/*  --->  API ASP.NET Core : 127.0.0.1:5000
    `-- /*      --->  Next.js          : 127.0.0.1:3000
                              |
                              `--> SQL Server
```

Le port `5000` ne doit pas être exposé sur le réseau. Le port `3000` ne doit pas être exposé non plus : seul IIS doit être accessible par les utilisateurs.

## 2. Préparer le serveur

### 2.1 Informations à réunir

Avant de commencer, noter les valeurs suivantes :

| Information | Exemple | Valeur à utiliser |
|---|---|---|
| Nom DNS du site | `coris.mondomaine.fr` | à fournir par l'administrateur réseau |
| Dossier de déploiement | `C:\Sites\Coris` | peut être changé |
| Instance SQL Server | `SQLSERVER01\SQLEXPRESS` | à confirmer |
| Base de données | `IncidentsDSI` | nom utilisé par le projet |
| Port public de test | `8080` | uniquement pour un test local |
| Ports internes | `5000` et `3000` | ne pas ouvrir dans le pare-feu réseau |

Pour une vraie mise en production, prévoir aussi :

- un certificat TLS pour le nom DNS du site ;
- un compte de service Windows dédié au processus Next.js ;
- une sauvegarde SQL Server ;
- un accès administrateur au serveur IIS et un accès d'administration à SQL Server.

### 2.2 Ouvrir PowerShell en administrateur

Depuis le menu Démarrer :

1. rechercher **PowerShell** ;
2. cliquer avec le bouton droit sur **Windows PowerShell** ;
3. choisir **Exécuter en tant qu'administrateur** ;
4. accepter la fenêtre UAC.

Vérifier que la console est bien élevée :

```powershell
$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
```

La commande doit afficher `True`.

Relancer également VS Code en administrateur si l'on utilise le terminal intégré pour exécuter les scripts IIS.

## 3. Installer les prérequis

Les quatre installations suivantes sont obligatoires :

1. le rôle IIS ;
2. IIS URL Rewrite ;
3. Application Request Routing (ARR) ;
4. le .NET 10 Hosting Bundle.

Node.js LTS est également nécessaire pour exécuter le frontend Next.js.

### 3.1 Installer IIS

Sur **Windows Server**, ouvrir **Server Manager** puis :

1. **Add roles and features** ;
2. installation basée sur les rôles ;
3. sélectionner le serveur ;
4. cocher **Web Server (IIS)** ;
5. conserver les composants proposés ;
6. installer, puis redémarrer si Windows le demande.

Sur Windows 10/11 de développement, la commande suivante peut être utilisée dans PowerShell administrateur :

```powershell
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole,IIS-WebServer,IIS-ManagementConsole -All
```

Vérifier ensuite :

```powershell
Test-Path "$env:windir\System32\inetsrv\appcmd.exe"
Get-Service W3SVC,WAS
```

`Test-Path` doit afficher `True`. Les services `W3SVC` et `WAS` doivent être démarrés.

### 3.2 Installer URL Rewrite et ARR

Télécharger et installer les versions x64 depuis les pages officielles :

- [IIS URL Rewrite](https://www.iis.net/downloads/microsoft/url-rewrite) ;
- [Application Request Routing](https://www.iis.net/downloads/microsoft/application-request-routing).

Installer d'abord **URL Rewrite**, puis **ARR**. Redémarrer IIS après les deux installations.

On peut aussi lancer le script fourni par le projet. Il demande automatiquement l'élévation UAC et télécharge les installateurs x64 :

```powershell
Set-ExecutionPolicy -Scope Process Bypass
Set-Location "C:\chemin\vers\Apllication banque"
.\deploy\iis\install-prerequisites.ps1
```

Le script installe les versions précisées dans son code. Pour un serveur de production, vérifier les versions et les signatures des installateurs avant de l'exécuter.

Activer ensuite le proxy ARR au niveau du serveur IIS :

```powershell
$appcmd = "$env:windir\System32\inetsrv\appcmd.exe"
& $appcmd set config -section:system.webServer/proxy /enabled:true
```

Sans cette option, IIS ne pourra pas relayer les requêtes vers Next.js et l'API.

### 3.3 Installer le .NET 10 Hosting Bundle

Télécharger le **Hosting Bundle** depuis la page officielle [.NET 10 Download](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), et non uniquement le SDK ou le Runtime.

Le Hosting Bundle installe notamment :

- le runtime ASP.NET Core ;
- le module **ASP.NET Core Module V2** utilisé par IIS.

Lancer l'installateur en administrateur, puis redémarrer IIS :

```powershell
& "$env:windir\System32\inetsrv\appcmd.exe" stop site "Default Web Site"
Restart-Service WAS -Force
Restart-Service W3SVC -Force
```

En cas de doute, redémarrer complètement le serveur. La documentation Microsoft de référence est [Publier une application ASP.NET Core sur IIS](https://learn.microsoft.com/en-us/aspnet/core/tutorials/publish-to-iis?view=aspnetcore-10.0).

Vérifier la présence du module :

```powershell
& "$env:windir\System32\inetsrv\appcmd.exe" list modules | Select-String AspNetCoreModule
dotnet --list-runtimes | Select-String "Microsoft.AspNetCore.App 10"
```

### 3.4 Installer Node.js LTS

Installer la version LTS x64 depuis [nodejs.org](https://nodejs.org/en/download). Fermer puis rouvrir PowerShell après l'installation.

Vérifier :

```powershell
node --version
npm --version
```

Le projet utilise les dépendances verrouillées dans `frontend\package-lock.json`. La publication doit donc utiliser `npm ci`, ce que fait le script fourni.

## 4. Préparer SQL Server

### 4.1 Choisir la chaîne de connexion

Le modèle de configuration se trouve dans [appsettings.Production.example.json](../../backend/appsettings.Production.example.json).

Exemple avec authentification Windows :

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SQLSERVER01\\SQLEXPRESS;Database=IncidentsDSI;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Cors": {
    "AllowedOrigins": [ "https://coris.mondomaine.fr" ]
  },
  "Database": {
    "ApplyMigrations": false
  },
  "Swagger": {
    "Enabled": false
  },
  "HttpsRedirection": {
    "Enabled": false
  }
}
```

Adapter `Server`, l'instance, le nom de base et le nom DNS. Ne jamais copier un vrai mot de passe dans Git ou dans `appsettings.Production.example.json`.

### 4.2 Créer la base avant le premier démarrage

En production, créer la base et appliquer les migrations avec un compte d'administration contrôlé. Depuis la racine du dépôt :

```powershell
dotnet tool install --global dotnet-ef --version 10.*
dotnet ef database update `
  --project .\backend\IncidentsDsi.Api.csproj `
  --connection "Server=SQLSERVER01\SQLEXPRESS;Database=IncidentsDSI;Trusted_Connection=True;TrustServerCertificate=True;"
```

Si `dotnet-ef` est déjà installé, la première commande n'est pas nécessaire. Vérifier ensuite que la base `IncidentsDSI` existe et que les migrations ont été appliquées.

Conserver `Database.ApplyMigrations=false` dans la configuration de production. Cela évite qu'un démarrage applicatif tente de modifier le schéma avec les droits du processus IIS.

### 4.3 Donner les droits SQL au pool IIS

L'API sera exécutée par l'identité Windows `IIS APPPOOL\CorisApiPool`. Dans SQL Server Management Studio, exécuter avec un compte SQL administrateur :

```sql
USE [master];
CREATE LOGIN [IIS APPPOOL\CorisApiPool] FROM WINDOWS;
GO

USE [IncidentsDSI];
CREATE USER [IIS APPPOOL\CorisApiPool]
  FOR LOGIN [IIS APPPOOL\CorisApiPool];
GO

ALTER ROLE [db_datareader] ADD MEMBER [IIS APPPOOL\CorisApiPool];
ALTER ROLE [db_datawriter] ADD MEMBER [IIS APPPOOL\CorisApiPool];
GO
```

Ces droits sont un exemple de départ. Les réduire ou les compléter selon les besoins réels de l'application. Ne donner `db_owner` au pool IIS que temporairement, par exemple pour un premier test local avec des migrations automatiques ; ce n'est pas le réglage recommandé en production.

## 5. Publier l'application

### 5.1 Depuis la racine du dépôt

Ouvrir PowerShell dans le dossier qui contient `backend`, `frontend` et `deploy`, puis exécuter :

```powershell
Set-Location "C:\chemin\vers\Apllication banque"
Set-ExecutionPolicy -Scope Process Bypass
.\deploy\iis\publish.ps1 -DeployRoot "C:\Sites\Coris" -PublicApiUrl "/api"
```

Le script :

1. exécute `dotnet publish` en configuration `Release` ;
2. installe les dépendances frontend avec `npm ci` ;
3. construit Next.js en mode standalone ;
4. injecte `NEXT_PUBLIC_API_URL=/api` dans le frontend ;
5. copie le serveur Next.js, les fichiers statiques et `web.config` dans `C:\Sites\Coris\frontend`.

À la fin, vérifier les fichiers essentiels :

```powershell
Test-Path "C:\Sites\Coris\api\web.config"
Test-Path "C:\Sites\Coris\frontend\server.js"
Test-Path "C:\Sites\Coris\frontend\frontend-web.config"
Test-Path "C:\Sites\Coris\frontend\start-frontend.ps1"
```

La dernière commande doit être `True` également.

### 5.2 Publier sur un serveur distant

La machine de build doit disposer de `dotnet`, `node` et `npm`. Le serveur IIS n'a pas besoin du SDK .NET ni des sources du dépôt : il a besoin du Hosting Bundle, de Node.js et des dossiers publiés.

1. Exécuter `publish.ps1` dans un dossier de sortie local.
2. Copier `api` et `frontend` vers le serveur, par exemple dans `C:\Sites\Coris`.
3. Copier `appsettings.Production.json` sur le serveur, dans `C:\Sites\Coris\api`.
4. Ne pas copier les secrets dans le dépôt source.

Attention : `publish.ps1` reconstruit et remplace le contenu du dossier frontend cible. Faire une sauvegarde avant toute republication sur une installation existante.

## 6. Configurer IIS

Les noms utilisés par les scripts sont `CorisApi`, `CorisApiPool`, `Coris`, et `CorisFrontendPool`. Les créer avec ces noms pour pouvoir réutiliser les vérifications fournies.

### 6.1 Créer les pools d'applications

Dans **IIS Manager** :

1. ouvrir **Application Pools** ;
2. cliquer sur **Add Application Pool** ;
3. créer `CorisApiPool` ;
4. créer `CorisFrontendPool` ;
5. pour les deux pools, choisir **No Managed Code** ;
6. conserver une identité dédiée, idéalement `ApplicationPoolIdentity` pour l'API.

Pour l'API, l'identité par défaut correspond à `IIS APPPOOL\CorisApiPool`, utilisée dans la configuration SQL ci-dessus.

### 6.2 Créer le site API `CorisApi`

Dans **Sites > Add Website** :

- **Site name** : `CorisApi` ;
- **Physical path** : `C:\Sites\Coris\api` ;
- **Application pool** : `CorisApiPool` ;
- **Type** : `http` ;
- **IP address** : `127.0.0.1` ;
- **Port** : `5000` ;
- **Host name** : laisser vide.

Le `web.config` de l'API est généré par `dotnet publish`. Ne pas le remplacer par le `web.config` du frontend.

Tester directement l'API :

```powershell
Invoke-WebRequest "http://127.0.0.1:5000/swagger/index.html" -UseBasicParsing
```

Swagger est désactivé par défaut en production. Si la réponse est `404` mais que l'application est démarrée, cela peut être normal ; vérifier alors un endpoint API autorisé.

### 6.3 Créer le site frontend `Coris`

Dans **Sites > Add Website** :

- **Site name** : `Coris` ;
- **Physical path** : `C:\Sites\Coris\frontend` ;
- **Application pool** : `CorisFrontendPool` ;
- **IP address** : `All Unassigned` ;
- pour un test local : **Type** `http`, port `8080` ;
- en production : **Type** `https`, port `443`, nom DNS et certificat TLS.

Le fichier `frontend\web.config` doit être présent à la racine du site. Il utilise URL Rewrite et ARR pour envoyer :

- `/api/...` vers `http://127.0.0.1:5000/api/...` ;
- toutes les autres URL vers `http://127.0.0.1:3000/...`.

Le port 8080 est pratique pour un test sur le serveur. Il ne remplace pas le binding HTTPS de production.

### 6.4 Accorder les droits sur les fichiers

L'API et IIS doivent pouvoir lire les fichiers publiés :

```powershell
icacls "C:\Sites\Coris\api" /grant "IIS AppPool\CorisApiPool:(OI)(CI)(RX)" /T
icacls "C:\Sites\Coris\frontend" /grant "IIS AppPool\CorisFrontendPool:(OI)(CI)(RX)" /T
```

Si un compte de service distinct est choisi pour Next.js, lui accorder au minimum la lecture/exécution sur `C:\Sites\Coris\frontend` et l'écriture sur le fichier de log si la journalisation est conservée.

## 7. Démarrer Next.js de façon fiable

IIS ne lance pas automatiquement le serveur Node.js standalone. Un processus doit écouter en permanence sur `127.0.0.1:3000`.

### Recommandation de production : service Windows

Utiliser un gestionnaire de service Windows tel que [NSSM](https://www.nssm.cc/download) ou WinSW. NSSM n'est pas fourni dans le dépôt ; le télécharger depuis sa source officielle, puis ouvrir PowerShell en administrateur dans le dossier qui contient `nssm.exe`.

Exemple avec un fichier `nssm.exe` x64 :

```powershell
$nssm = "C:\Outils\nssm\win64\nssm.exe"
$frontend = "C:\Sites\Coris\frontend"
$powershell = "$env:WINDIR\System32\WindowsPowerShell\v1.0\powershell.exe"

& $nssm install CorisFrontend $powershell `
  "-NoProfile -ExecutionPolicy Bypass -File `"$frontend\start-frontend.ps1`""
& $nssm set CorisFrontend AppDirectory $frontend
& $nssm set CorisFrontend DisplayName "Coris - Frontend Next.js"
& $nssm set CorisFrontend Start SERVICE_AUTO_START
& $nssm set CorisFrontend AppExit Default Restart
& $nssm start CorisFrontend
```

Vérifier que le service est démarré et que le port répond :

```powershell
Get-Service CorisFrontend
Get-NetTCPConnection -LocalPort 3000 -State Listen
Invoke-WebRequest "http://127.0.0.1:3000/" -UseBasicParsing
```

Le script `start-frontend.ps1` fixe `PORT=3000`, `HOSTNAME=127.0.0.1` et écrit les erreurs dans `C:\Sites\Coris\frontend\node.log`.

### Test local rapide

Le script [deploy-local.ps1](deploy-local.ps1) peut publier l'application, créer les deux sites et créer une tâche planifiée pour lancer Next.js avec le compte Windows courant :

```powershell
.\deploy\iis\deploy-local.ps1 -DeployRoot "C:\Sites\Coris" -FrontendPort 8080
```

Cette méthode est utile pour une démonstration ou un test sur une machine de développement. Pour une production, préférer un service Windows : une tâche planifiée interactive dépend de la session et de la connexion de l'utilisateur.

Si une installation locale a été interrompue, [finalize-local.ps1](finalize-local.ps1) peut redémarrer les sites et refaire les vérifications :

```powershell
.\deploy\iis\finalize-local.ps1 -FrontendPort 8080
```

## 8. Configurer DNS, HTTPS et le pare-feu

### 8.1 DNS et certificat

1. Faire pointer `coris.mondomaine.fr` vers l'adresse IP du serveur IIS.
2. Importer le certificat dans **IIS > Server Certificates**.
3. Modifier le binding du site `Coris` en `https`, port `443`.
4. sélectionner le certificat et le nom d'hôte DNS.
5. conserver un binding HTTP uniquement si une redirection vers HTTPS est configurée.

Le TLS est terminé par IIS. C'est la raison pour laquelle `HttpsRedirection.Enabled` est à `false` dans l'exemple de configuration : l'API ne reçoit que les appels internes IIS en HTTP. Si l'API est exposée directement en HTTPS, appliquer une configuration différente.

### 8.2 Pare-feu

Autoriser uniquement le trafic réellement nécessaire :

- `443` depuis le réseau des utilisateurs ;
- éventuellement `80` pour la redirection HTTP vers HTTPS ;
- `5000` et `3000` uniquement en local, jamais en entrée depuis le réseau.

Vérifier les écoutes locales :

```powershell
Get-NetTCPConnection -State Listen |
  Where-Object { $_.LocalPort -in 80,443,3000,5000,8080 }
```

## 9. Vérifier le fonctionnement complet

### 9.1 Vérifications locales

Exécuter sur le serveur IIS :

```powershell
Test-NetConnection 127.0.0.1 -Port 5000
Test-NetConnection 127.0.0.1 -Port 3000

Invoke-WebRequest "http://127.0.0.1:5000/swagger/index.html" -UseBasicParsing
Invoke-WebRequest "http://127.0.0.1:3000/" -UseBasicParsing
```

Pour le test local avec le binding 8080 :

```powershell
Invoke-WebRequest "http://localhost:8080/" -UseBasicParsing
Invoke-WebRequest "http://localhost:8080/api/referentiels" -UseBasicParsing
```

Les réponses attendues sont des statuts HTTP `200` ou, pour un endpoint volontairement protégé/non disponible, un statut documenté par l'équipe applicative.

### 9.2 Vérification depuis un poste utilisateur

Ouvrir dans un navigateur :

```text
https://coris.mondomaine.fr/
```

Dans les outils développeur du navigateur, onglet **Network** :

1. la page principale doit répondre ;
2. les appels doivent aller vers `/api/...` sur le même domaine ;
3. il ne doit pas y avoir d'appel vers `localhost:3000` ou `127.0.0.1:5000` depuis le navigateur.

## 10. Dépannage

| Symptôme | Cause probable | Action |
|---|---|---|
| `500.19` sur le frontend | URL Rewrite absent ou `web.config` invalide | installer URL Rewrite, vérifier `C:\Sites\Coris\frontend\web.config`, puis redémarrer IIS |
| `502` sur le frontend | Node.js n'écoute pas sur le port 3000 | vérifier `Get-Service CorisFrontend`, le processus Node et `node.log` |
| `500.30` ou `502.5` sur l'API | Hosting Bundle absent, erreur .NET ou configuration | consulter l'Observateur d'événements, les logs IIS et `appsettings.Production.json` |
| erreur SQL au démarrage | mauvaise instance, base absente ou droits manquants | tester la chaîne de connexion et le login `IIS APPPOOL\CorisApiPool` |
| `CREATE DATABASE permission denied` | l'API tente d'appliquer une migration avec des droits insuffisants | créer la base et les migrations séparément, puis mettre `ApplyMigrations=false` |
| `/api/...` renvoie `404` ou part vers le mauvais serveur | URL frontend injectée incorrectement | reconstruire avec `-PublicApiUrl "/api"` |
| navigateur bloqué par CORS | origine absente de `Cors.AllowedOrigins` | ajouter l'URL HTTPS exacte, sans slash final inutile |
| le site fonctionne en 8080 mais pas en HTTPS | binding, certificat, DNS ou pare-feu | vérifier le binding IIS `443`, le certificat et la résolution DNS |
| port 3000 occupé | ancien processus Node encore actif | identifier le PID avec `Get-NetTCPConnection -LocalPort 3000` avant de l'arrêter |

Journaux utiles :

```powershell
Get-Content "C:\Sites\Coris\frontend\node.log" -Tail 100
Get-WinEvent -LogName Application -MaxEvents 50 |
  Where-Object ProviderName -match "IIS|ASP.NET|.NET Runtime"
```

Les logs IIS se trouvent généralement sous `C:\inetpub\logs\LogFiles`.

## 11. Mettre à jour ou revenir en arrière

Avant une mise à jour :

1. sauvegarder la base de données ;
2. sauvegarder `C:\Sites\Coris\api` et `C:\Sites\Coris\frontend` ;
3. noter la version actuellement en production ;
4. arrêter le service `CorisFrontend` et recycler le pool API ;
5. publier dans un nouveau dossier de version, par exemple `C:\Sites\Coris\releases\2026-08-31` ;
6. vérifier les fichiers et la configuration ;
7. basculer les chemins physiques IIS ou remplacer les dossiers ;
8. redémarrer le service Node et les sites IIS ;
9. refaire toutes les vérifications de la section 9.

Ne pas supprimer un ancien dossier de production avant d'avoir validé la nouvelle version. En cas d'échec, remettre les chemins IIS précédents et restaurer la base si une migration irréversible a été exécutée.

## 12. Contrôle de sécurité avant ouverture aux utilisateurs

- [ ] Le site public utilise HTTPS avec un certificat valide.
- [ ] Les ports 3000 et 5000 écoutent uniquement sur `127.0.0.1`.
- [ ] Le port SQL n'est pas exposé inutilement.
- [ ] Les secrets sont hors Git et hors des fichiers d'exemple.
- [ ] `Database.ApplyMigrations=false` en production.
- [ ] Swagger est désactivé en production sauf besoin temporaire.
- [ ] Le pool IIS de l'API possède uniquement les droits SQL nécessaires.
- [ ] Les sauvegardes SQL et la procédure de restauration ont été testées.
- [ ] Un service Windows ou un autre superviseur redémarre Next.js après un reboot.
- [ ] La politique d'authentification/autorisation de l'application a été validée avant exposition publique.

## 13. Scripts fournis

| Script | Utilité |
|---|---|
| [install-prerequisites.ps1](install-prerequisites.ps1) | installe IIS, URL Rewrite, ARR et le Hosting Bundle avec élévation UAC |
| [publish.ps1](publish.ps1) | construit et publie l'API et le frontend |
| [deploy-local.ps1](deploy-local.ps1) | crée une installation locale de test avec les sites IIS |
| [finalize-local.ps1](finalize-local.ps1) | redémarre une installation locale existante et vérifie ses ports |
| [start-frontend.ps1](start-frontend.ps1) | lance le serveur Next.js standalone sur le port 3000 |
| [frontend-web.config](frontend-web.config) | configure le reverse proxy IIS vers Next.js et l'API |

Pour une première installation locale, l'ordre recommandé est :

```powershell
.\deploy\iis\install-prerequisites.ps1
.\deploy\iis\deploy-local.ps1
```

Pour une production, utiliser plutôt `publish.ps1`, configurer IIS et SQL avec les valeurs du serveur, puis exécuter les vérifications manuellement.
