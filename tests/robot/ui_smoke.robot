*** Settings ***
Documentation    Parcours UI de fumée pour le frontend Next.js.
Library          Browser
Library          OperatingSystem
Suite Setup      Open Frontend
Suite Teardown   Close Browser
Test Timeout     60 seconds

*** Variables ***
${FRONTEND_URL}    http://localhost:3000
${HEADLESS}        ${True}

*** Test Cases ***
Le tableau de bord est accessible
    Get Text    h1    ==    Tableau de bord
    Wait For Elements State    role=link[name="Voir les incidents"]    visible
    Wait For Elements State    role=heading[name="Analyse des incidents"]    visible
    Wait For Elements State    role=tab[name="Par mois"]    visible
    Wait For Elements State    role=tab[name="Par type"]    visible
    Wait For Elements State    role=tab[name="Par applicatif"]    visible
    Wait For Elements State    role=button[name="Exporter"]    visible
    Wait For Elements State    css=svg[aria-label*="Histogramme"]    visible

L histogramme annuel est exportable
    Go To    ${FRONTEND_URL}
    Wait For Elements State    role=heading[name="Analyse des incidents"]    visible    30s
    ${promise}=    Promise To Wait For Download    saveAs=${OUTPUT DIR}${/}histogramme-incidents.svg
    Click    role=button[name="Exporter"]
    Wait For    ${promise}
    File Should Exist    ${OUTPUT DIR}${/}histogramme-incidents.svg

Les analyses par type et applicatif sont exportables par annee
    Go To    ${FRONTEND_URL}
    Wait For Elements State    role=heading[name="Analyse des incidents"]    visible    30s
    Click    role=tab[name="Par type"]
    Wait For Elements State    css=svg[aria-label*="Par type"]    visible
    ${type_download}=    Promise To Wait For Download    saveAs=${OUTPUT DIR}${/}incidents-par-type.svg
    Click    role=button[name="Exporter"]
    Wait For    ${type_download}
    File Should Exist    ${OUTPUT DIR}${/}incidents-par-type.svg
    Click    role=tab[name="Par applicatif"]
    Wait For Elements State    css=svg[aria-label*="Par applicatif"]    visible
    ${app_download}=    Promise To Wait For Download    saveAs=${OUTPUT DIR}${/}incidents-par-applicatif.svg
    Click    role=button[name="Exporter"]
    Wait For    ${app_download}
    File Should Exist    ${OUTPUT DIR}${/}incidents-par-applicatif.svg

Le tableau de bord reste coherent en mobile
    Set Viewport Size    390    844
    Go To    ${FRONTEND_URL}
    Wait For Elements State    role=heading[name="Tableau de bord"]    visible    30s
    Wait For Elements State    role=heading[name="Analyse des incidents"]    visible
    Wait For Elements State    role=tab[name="Par mois"]    visible
    Wait For Elements State    role=button[name="Exporter"]    visible
    ${no_overflow}=    Evaluate JavaScript    ${None}    () => document.documentElement.scrollWidth <= window.innerWidth + 2
    Should Be True    ${no_overflow}
    Set Viewport Size    1440    900

La page incidents affiche la recherche et les filtres
    Go To    ${FRONTEND_URL}/incidents
    Wait For Elements State    h1    visible    30s
    Get Text    h1    ==    Incidents
    Wait For Elements State    css=input[placeholder*="Rechercher"]    visible
    Wait For Elements State    text=Recherche & filtres    visible
    Wait For Elements State    text=Nouvel incident    visible

Le formulaire vide affiche les validations obligatoires
    Go To    ${FRONTEND_URL}/incidents/nouveau
    Wait For Elements State    h1    visible    30s
    Get Text    h1    ==    Nouvel incident
    Click    text=Créer l'incident
    Wait For Elements State    text=L'intitulé doit faire au moins 3 caractères    visible
    Wait For Elements State    text=La description doit faire au moins 10 caractères    visible

La page administration permet d ouvrir un referentiel
    Go To    ${FRONTEND_URL}/administration
    Wait For Elements State    h1    visible    30s
    Get Text    h1    ==    Paramètres
    Click    role=tab[name="Applications"]
    Wait For Elements State    role=heading[name="Applications"]    visible
    Click    role=button[name="Ajouter"]
    Wait For Elements State    css=[role="dialog"]    visible
    Wait For Elements State    css=input[placeholder="Nom de la valeur"]    visible

*** Keywords ***
Open Frontend
    New Browser    chromium    headless=${HEADLESS}
    New Context    viewport={"width": 1440, "height": 900}
    New Page    ${FRONTEND_URL}
    Wait For Elements State    h1    visible    30s
