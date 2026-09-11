# Base de démonstration IncidentsDSI

Le fichier [IncidentsDSI-demo-data.sql](IncidentsDSI-demo-data.sql) contient le jeu de données opérationnel de démonstration : incidents, projets, étapes, responsables et référentiels associés.

Il contient notamment :

- 48 incidents ;
- 37 projets ;
- 134 étapes de projet ;
- 56 associations projet/responsable.

## Restauration

Depuis la racine du dépôt, appliquer d’abord les migrations EF Core afin de créer le schéma :

```powershell
dotnet ef database update --project .\backend\IncidentsDsi.Api.csproj
```

Puis charger les données de démonstration :

```powershell
sqlcmd -S "localhost\SQLEXPRESS" -d IncidentsDSI -E -i .\backend\Database\IncidentsDSI-demo-data.sql
```

Le script remplace les données applicatives existantes du schéma `dbo`. L’exécuter uniquement sur une base de démonstration ou après avoir effectué une sauvegarde.

Le script [generate-demo-dump.ps1](generate-demo-dump.ps1) permet de régénérer l’export depuis la base locale.
