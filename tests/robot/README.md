# Tests Robot

Suites Robot Framework pour verifier l'application de gestion des incidents DSI.

## Prerequis

Le backend doit tourner sur:

```text
http://localhost:5000/api
```

Le frontend doit tourner sur:

```text
http://localhost:3000
```

Installer les dependances Python si necessaire:

```powershell
pip install robotframework robotframework-requests robotframework-browser
rfbrowser init
```

## Suites disponibles

- `api_contract.robot`: verifie les endpoints API principaux.
- `bug_probes.robot`: verifie les regressions importantes deja corrigees.
- `data_seed.robot`: ajoute des donnees via l'API, sans passer par un import Excel.
- `ui_smoke.robot`: verifie que l'interface s'ouvre, que le tableau de bord se charge et que l'histogramme peut etre exporte.

## Commandes utiles

Executer les tests API:

```powershell
py -m robot --outputdir tests/robot/results/api tests/robot/api_contract.robot
```

Executer les tests UI:

```powershell
py -m robot --outputdir tests/robot/results/ui tests/robot/ui_smoke.robot
```

Ajouter des donnees de test par l'API:

```powershell
py -m robot --outputdir tests/robot/results/data tests/robot/data_seed.robot
```

Executer toutes les suites:

```powershell
py -m robot --outputdir tests/robot/results tests/robot
```
