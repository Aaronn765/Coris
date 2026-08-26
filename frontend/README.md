# Frontend

Interface web Next.js de l'application de gestion des incidents DSI.

## Role

Le frontend permet de:

- consulter le tableau de bord des incidents;
- voir les derniers incidents;
- analyser les incidents par mois et par annee;
- exporter l'histogramme mensuel;
- rechercher, filtrer et paginer la liste des incidents;
- creer et modifier des incidents;
- gerer les referentiels utilises par le backend.
- consulter et filtrer les projets DSI;
- gérer les responsables multiples et les étapes de chaque projet;
- afficher les indicateurs projets dans le second onglet du tableau de bord.

## Configuration

L'URL de l'API est lue depuis `NEXT_PUBLIC_API_URL`.

Exemple local:

```env
NEXT_PUBLIC_API_URL=http://localhost:5000/api
```

Si la variable n'est pas definie, l'interface utilise l'URL par defaut prevue dans le code.

## Commandes

Installer les dependances:

```powershell
npm install
```

Lancer en local:

```powershell
npm run dev
```

Verifier le build:

```powershell
npm run build
```

Adresse locale:

```text
http://localhost:3000
```
