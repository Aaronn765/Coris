*** Settings ***
Documentation    Contrat HTTP nominal, validation, filtres, dashboard, export et administration.
Resource         resources/api_keywords.resource
Suite Setup      Open API Session
Suite Teardown   Close API Sessions
Test Timeout     45 seconds

*** Test Cases ***
Referentiels actifs sont exposés et triés par nom
    [Template]    Active referentiel should be sorted
    responsables
    applications
    types-incidents
    entites
    criticites
    risques
    statuts

Les alias de type incident sont acceptés
    ${first}=    Get Referentiel List    typesincident
    ${second}=    Get Referentiel List    type-incidents
    ${first_count}=    Evaluate    len($first)
    ${second_count}=    Evaluate    len($second)
    Should Be Equal As Integers    ${first_count}    ${second_count}
    ${first_names}=    Evaluate    [x["nom"] for x in $first]
    ${second_names}=    Evaluate    [x["nom"] for x in $second]
    Should Be Equal    ${first_names}    ${second_names}

Un referentiel existant est lisible par identifiant
    ${items}=    Get Referentiel List    applications
    ${item}=    Get From List    ${items}    0
    ${id}=    Get From Dictionary    ${item}    id
    ${response}=    GET On Session    api    /referentiels/applications/${id}    expected_status=any
    Should Be Equal As Integers    ${response.status_code}    200
    ${body}=    Get JSON    ${response}
    Should Be Equal As Integers    ${body}[id]    ${id}

Une ressource inexistante renvoie 404
    ${response}=    GET On Session    api    /incidents/2147483647    expected_status=any
    Should Be Equal As Integers    ${response.status_code}    404

Un type de referentiel inconnu renvoie un probleme JSON
    ${response}=    GET On Session    api    /referentiels/inconnu    expected_status=any
    Assert Problem Response    ${response}    400

Creation referentiel tronque les espaces et renvoie Location
    ${token}=    Make Token
    ${padded_name}=    Set Variable    ${SPACE}${SPACE}${token}${SPACE}${SPACE}
    ${payload}=    Create Dictionary    nom=${padded_name}    actif=${True}
    ${response}=    POST On Session    api    /referentiels/applications    json=${payload}    expected_status=any
    Should Be Equal As Integers    ${response.status_code}    201
    ${location}=    Get Header    ${response}    Location
    Should Contain    ${location}    /api/referentiels/applications/
    ${body}=    Get JSON    ${response}
    Should Be Equal    ${body}[nom]    ${token}
    ${id}=    Get From Dictionary    ${body}    id
    ${cleanup}=    Create Dictionary    nom=${token}    actif=${False}
    ${update}=    PUT On Session    api    /referentiels/applications/${id}    json=${cleanup}    expected_status=any
    Should Be Equal As Integers    ${update.status_code}    200

Creation referentiel dupliquee est refusee
    ${token}=    Make Token
    ${payload}=    Create Dictionary    nom=${token}    actif=${True}
    ${first}=    POST On Session    api    /referentiels/risques    json=${payload}    expected_status=any
    Should Be Equal As Integers    ${first.status_code}    201
    ${second}=    POST On Session    api    /referentiels/risques    json=${payload}    expected_status=any
    Assert Problem Response    ${second}    400

Desactivation logique masque la valeur des listes actives
    ${token}=    Make Token
    ${payload}=    Create Dictionary    nom=${token}    actif=${True}
    ${created_response}=    POST On Session    api    /referentiels/applications    json=${payload}    expected_status=any
    Should Be Equal As Integers    ${created_response.status_code}    201
    ${created}=    Get JSON    ${created_response}
    ${id}=    Get From Dictionary    ${created}    id
    ${disabled}=    Create Dictionary    nom=${token}    actif=${False}
    ${updated_response}=    PUT On Session    api    /referentiels/applications/${id}    json=${disabled}    expected_status=any
    Should Be Equal As Integers    ${updated_response.status_code}    200
    ${active}=    Get Referentiel List    applications
    ${all}=    Get Referentiel List    applications    ${True}
    ${active_ids}=    Evaluate    [x["id"] for x in $active]
    ${all_ids}=    Evaluate    [x["id"] for x in $all]
    Should Not Contain    ${active_ids}    ${id}
    Should Contain    ${all_ids}    ${id}

Liste incidents renvoie une enveloppe paginee et les details
    ${body}=    Get Incident List    ?page=1&pageSize=3
    FOR    ${key}    IN    items    page    pageSize    totalCount    totalPages
        Dictionary Should Contain Key    ${body}    ${key}
    END
    ${items}=    Get From Dictionary    ${body}    items
    ${has_at_most_three}=    Evaluate    len($items) <= 3
    Should Be True    ${has_at_most_three}
    ${has_items}=    Evaluate    len($items) > 0
    IF    ${has_items}
        ${incident}=    Get From List    ${items}    0
        Assert Incident Has Required Details    ${incident}
    END

Creation incident genere un numero et calcule la duree exacte
    ${token}=    Make Token
    ${payload}=    Create Valid Incident Payload    ${token}    2026-08-10T00:00:00Z    2026-08-12T00:00:00Z
    ${incident}=    Create Incident    ${payload}
    Should Match Regexp    ${incident}[numero]    ^INC-2026-[0-9]{4}$
    Should Be Equal As Integers    ${incident}[dureeMinutes]    2880
    Should Be Equal As Integers    ${incident}[dureeJours]    2
    Should Be Equal    ${incident}[application][nom]    AMPLITUDE
    Should Be Equal    ${incident}[responsableN1][nom]    ARSENE

Deux incidents consecutifs ont des numeros consecutifs par annee
    ${token1}=    Make Token
    ${token2}=    Make Token
    ${first}=    Create Open Incident    ${token1}    2026-08-11T00:00:00Z
    ${second}=    Create Open Incident    ${token2}    2026-08-12T00:00:00Z
    ${first_seq}=    Evaluate    int($first["numero"].split("-")[-1])
    ${second_seq}=    Evaluate    int($second["numero"].split("-")[-1])
    ${expected_seq}=    Evaluate    $first_seq + 1
    Should Be Equal As Integers    ${second_seq}    ${expected_seq}

Une date de fin absente produit une duree nulle
    ${token}=    Make Token
    ${incident}=    Create Open Incident    ${token}    2026-08-13T00:00:00Z
    ${date_fin}=    Evaluate    $incident.get("dateFin")
    ${duree_minutes}=    Evaluate    $incident.get("dureeMinutes")
    ${duree_jours}=    Evaluate    $incident.get("dureeJours")
    Should Be Equal    ${date_fin}    ${None}
    Should Be Equal    ${duree_minutes}    ${None}
    Should Be Equal    ${duree_jours}    ${None}

Des dates egales produisent zero minute et zero jour
    ${token}=    Make Token
    ${payload}=    Create Valid Incident Payload    ${token}    2026-08-14T12:34:00Z    2026-08-14T12:34:00Z
    ${incident}=    Create Incident    ${payload}
    Should Be Equal As Integers    ${incident}[dureeMinutes]    0
    Should Be Equal As Integers    ${incident}[dureeJours]    0

Creation nettoie les champs optionnels vides
    ${token}=    Make Token
    ${payload}=    Create Valid Incident Payload    ${token}
    Set To Dictionary    ${payload}    impact=       cause=       solution=       actionsMenees=       actionsEnCours=       mesuresPreventives=
    ${incident}=    Create Incident    ${payload}
    ${impact}=    Evaluate    $incident.get("impact")
    ${cause}=    Evaluate    $incident.get("cause")
    ${solution}=    Evaluate    $incident.get("solution")
    ${actions}=    Evaluate    $incident.get("actionsMenees")
    Should Be Equal    ${impact}    ${None}
    Should Be Equal    ${cause}    ${None}
    Should Be Equal    ${solution}    ${None}
    Should Be Equal    ${actions}    ${None}

Modification incident conserve le numero et remet a jour les champs
    ${token}=    Make Token
    ${created}=    Create Open Incident    ${token}    2026-08-15T00:00:00Z
    ${payload}=    Create Valid Incident Payload    ${token}-UPDATED    2026-08-15T00:00:00Z    2026-08-16T01:30:00Z
    Set To Dictionary    ${payload}    statutId=2    applicationId=1    responsableN2Id=0
    Remove From Dictionary    ${payload}    responsableN2Id
    ${response}=    PUT On Session    api    /incidents/${created}[id]    json=${payload}    expected_status=any
    Should Be Equal As Integers    ${response.status_code}    200
    ${updated}=    Get JSON    ${response}
    Should Be Equal    ${updated}[numero]    ${created}[numero]
    Should Be Equal    ${updated}[intitule]    ${token}-UPDATED
    Should Be Equal As Integers    ${updated}[applicationId]    1
    Should Be Equal    ${updated}[statut][nom]    Clôturé
    ${responsable_n2}=    Evaluate    $updated.get("responsableN2Id")
    Should Be Equal    ${responsable_n2}    ${None}

Dates invalides sont refusees
    ${token}=    Make Token
    ${payload}=    Create Valid Incident Payload    ${token}    2026-08-20T00:00:00Z    2026-08-19T23:59:59Z
    ${response}=    POST On Session    api    /incidents    json=${payload}    expected_status=any
    Assert Problem Response    ${response}    400

Les champs texte obligatoires sont valides
    ${payload}=    Create Valid Incident Payload    ab    2026-08-21T00:00:00Z    2026-08-21T00:00:00Z
    ${response}=    POST On Session    api    /incidents    json=${payload}    expected_status=any
    Assert Problem Response    ${response}    400
    ${payload2}=    Create Valid Incident Payload    ${EMPTY}    2026-08-21T00:00:00Z    2026-08-21T00:00:00Z
    Set To Dictionary    ${payload2}    description=courte
    ${response2}=    POST On Session    api    /incidents    json=${payload2}    expected_status=any
    Assert Problem Response    ${response2}    400

Les longueurs maximales des champs sont respectees
    ${token}=    Make Token
    ${long_title}=    Evaluate    "T" * 301
    ${payload}=    Create Valid Incident Payload    ${token}
    Set To Dictionary    ${payload}    intitule    ${long_title}
    ${response}=    POST On Session    api    /incidents    json=${payload}    expected_status=any
    Assert Problem Response    ${response}    400
    ${long_description}=    Evaluate    "D" * 10001
    ${payload2}=    Create Valid Incident Payload    ${token}-DESCRIPTION
    Set To Dictionary    ${payload2}    description    ${long_description}
    ${response2}=    POST On Session    api    /incidents    json=${payload2}    expected_status=any
    Assert Problem Response    ${response2}    400

Les parametres de requete invalides sont refuses
    ${response}=    GET On Session    api    url=/incidents?page=not-a-number&pageSize=20    expected_status=any
    Assert Problem Response    ${response}    400

Une mise a jour d un incident inexistant renvoie 404
    ${token}=    Make Token
    ${payload}=    Create Valid Incident Payload    ${token}
    ${response}=    PUT On Session    api    /incidents/2147483647    json=${payload}    expected_status=any
    Should Be Equal As Integers    ${response.status_code}    404

Les identifiants de referentiel inexistants sont refuses
    [Template]    Invalid incident reference should be rejected
    typeIncidentId
    applicationId
    entiteId
    criticiteId
    risqueId
    responsableN1Id
    responsableN2Id
    statutId

Recherche couvre les champs texte et les referentiels
    ${token}=    Make Token
    ${application_name}=    Set Variable    ${token}-APP
    ${entite_name}=    Set Variable    ${token}-ENTITY
    ${responsable_name}=    Set Variable    ${token}-RESP
    ${application_id}=    Ensure Referentiel    applications    ${application_name}
    ${entite_id}=    Ensure Referentiel    entites    ${entite_name}
    ${responsable_id}=    Ensure Referentiel    responsables    ${responsable_name}
    ${payload}=    Create Valid Incident Payload    ${token}-TITLE
    Set To Dictionary    ${payload}
    ...    applicationId=${application_id}
    ...    entiteId=${entite_id}
    ...    responsableN1Id=${responsable_id}
    ...    responsableN2Id=${responsable_id}
    ...    description=${token}-DESCRIPTION
    ...    cause=${token}-CAUSE
    ...    solution=${token}-SOLUTION
    ...    actionsMenees=${token}-ACTIONS
    ${incident}=    Create Incident    ${payload}
    FOR    ${needle}    IN    ${token}-TITLE    ${token}-DESCRIPTION    ${token}-CAUSE    ${token}-SOLUTION    ${token}-ACTIONS    ${application_name}    ${entite_name}    ${responsable_name}
        ${body}=    Get Incident List    ?search=${needle}&pageSize=100
        Assert Every Incident Matches Search    ${body}    ${needle}
    END

Les filtres par identifiant isolent les incidents
    ${token}=    Make Token
    ${payload}=    Create Valid Incident Payload    ${token}
    ${incident}=    Create Incident    ${payload}
    FOR    ${parameter}    ${value}    IN
    ...    statutId    1
    ...    typeIncidentId    2
    ...    applicationId    2
    ...    entiteId    12
    ...    criticiteId    1
    ...    risqueId    1
    ...    responsableId    1
        ${body}=    Get Incident List    ?search=${token}&${parameter}=${value}&pageSize=100
        ${items}=    Get From Dictionary    ${body}    items
        Should Not Be Empty    ${items}
        FOR    ${item}    IN    @{items}
            ${title}=    Get From Dictionary    ${item}    intitule
            Should Contain    ${title}    ${token}
        END
    END

Le filtre responsable couvre N1 et N2
    ${token}=    Make Token
    ${payload}=    Create Valid Incident Payload    ${token}
    Set To Dictionary    ${payload}    responsableN1Id=1    responsableN2Id=14
    ${incident}=    Create Incident    ${payload}
    ${n1}=    Get Incident List    ?search=${token}&responsableId=1&pageSize=100
    ${n2}=    Get Incident List    ?search=${token}&responsableId=14&pageSize=100
    Should Be Equal As Integers    ${n1}[totalCount]    1
    Should Be Equal As Integers    ${n2}[totalCount]    1

Les dates de debut et de fin sont inclusives sur les jours
    ${token}=    Make Token
    ${before}=    Create Open Incident    ${token}-BEFORE    2026-08-30T23:59:59Z
    ${inside}=    Create Open Incident    ${token}-INSIDE    2026-08-31T12:00:00Z
    ${after}=    Create Open Incident    ${token}-AFTER    2026-09-01T00:00:00Z
    ${body}=    Get Incident List    ?search=${token}&dateDebut=2026-08-31&dateFin=2026-08-31&pageSize=100
    ${items}=    Get From Dictionary    ${body}    items
    Should Be Equal As Integers    ${body}[totalCount]    1
    Should Be Equal    ${items}[0][intitule]    ${token}-INSIDE

La pagination borne page et pageSize
    ${body}=    Get Incident List    ?page=0&pageSize=0
    Should Be Equal As Integers    ${body}[page]    1
    Should Be Equal As Integers    ${body}[pageSize]    1
    ${body2}=    Get Incident List    ?page=-5&pageSize=999
    Should Be Equal As Integers    ${body2}[page]    1
    Should Be Equal As Integers    ${body2}[pageSize]    100

La pagination calcule le nombre de pages
    ${token}=    Make Token
    FOR    ${index}    IN RANGE    3
        Create Open Incident    ${token}-${index}    2026-09-02T00:00:00Z
    END
    ${body}=    Get Incident List    ?search=${token}&page=1&pageSize=2&sortBy=numero&sortDirection=asc
    Should Be Equal As Integers    ${body}[totalCount]    3
    Should Be Equal As Integers    ${body}[totalPages]    2
    ${items}=    Get From Dictionary    ${body}    items
    ${item_count}=    Evaluate    len($items)
    Should Be Equal As Integers    ${item_count}    2

Le tri par numero et intitule respecte la direction
    ${token}=    Make Token
    Create Open Incident    ${token}-B    2026-09-03T00:00:00Z
    Create Open Incident    ${token}-A    2026-09-04T00:00:00Z
    ${asc}=    Get Incident List    ?search=${token}&sortBy=intitule&sortDirection=asc&pageSize=100
    ${desc}=    Get Incident List    ?search=${token}&sortBy=intitule&sortDirection=desc&pageSize=100
    Should Be Equal    ${asc}[items][0][intitule]    ${token}-A
    Should Be Equal    ${desc}[items][0][intitule]    ${token}-B

Le filtre de duree exclut les incidents ouverts
    ${token}=    Make Token
    ${closed}=    Create Valid Incident Payload    ${token}-CLOSED    2026-09-05T00:00:00Z    2026-09-06T00:00:00Z
    Create Incident    ${closed}
    Create Open Incident    ${token}-OPEN    2026-09-05T00:00:00Z
    ${body}=    Get Incident List    ?search=${token}&dureeMinMinutes=1440&dureeMaxMinutes=1440&pageSize=100
    Should Be Equal As Integers    ${body}[totalCount]    1
    Should Be Equal    ${body}[items][0][intitule]    ${token}-CLOSED

Le dashboard respecte une periode et agrege les compteurs
    ${token}=    Make Token
    ${before_response}=    GET On Session    api    url=/dashboard?dateDebut=2026-10-01&dateFin=2026-10-31    expected_status=any
    Should Be Equal As Integers    ${before_response.status_code}    200
    ${before}=    Get JSON    ${before_response}
    ${open}=    Create Open Incident    ${token}-OPEN    2026-10-10T00:00:00Z
    ${closed_payload}=    Create Valid Incident Payload    ${token}-CLOSED    2026-10-11T00:00:00Z    2026-10-11T00:00:00Z
    Set To Dictionary    ${closed_payload}    statutId=2
    ${closed}=    Create Incident    ${closed_payload}
    ${stats}=    GET On Session    api    url=/dashboard?dateDebut=2026-10-01&dateFin=2026-10-31    expected_status=any
    Should Be Equal As Integers    ${stats.status_code}    200
    ${body}=    Get JSON    ${stats}
    ${expected_total}=    Evaluate    $before["totalIncidents"] + 2
    ${expected_open}=    Evaluate    $before["incidentsEnCours"] + 1
    ${expected_closed}=    Evaluate    $before["incidentsClotures"] + 1
    Should Be Equal As Integers    ${body}[totalIncidents]    ${expected_total}
    Should Be Equal As Integers    ${body}[incidentsEnCours]    ${expected_open}
    Should Be Equal As Integers    ${body}[incidentsClotures]    ${expected_closed}
    ${evolution}=    Evaluate    [x for x in $body["evolution"] if x["annee"] == 2026 and x["mois"] == 10]
    ${before_evolution}=    Evaluate    [x for x in $before["evolution"] if x["annee"] == 2026 and x["mois"] == 10]
    ${before_count}=    Set Variable If    ${before_evolution}    ${before_evolution}[0][count]    0
    ${expected_evolution}=    Evaluate    int($before_count) + 2
    Should Be Equal As Integers    ${evolution}[0][count]    ${expected_evolution}

Le dashboard vide renvoie zero sans erreur
    ${response}=    GET On Session    api    url=/dashboard?dateDebut=1900-01-01&dateFin=1900-01-02    expected_status=any
    Should Be Equal As Integers    ${response.status_code}    200
    ${body}=    Get JSON    ${response}
    Should Be Equal As Integers    ${body}[totalIncidents]    0
    Should Be Equal As Integers    ${body}[incidentsEnCours]    0
    Should Be Equal As Integers    ${body}[incidentsClotures]    0
    Should Be Empty    ${body}[evolution]

Export Excel renvoie un classeur filtrable
    ${token}=    Make Token
    Create Open Incident    ${token}-EXPORT    2026-11-01T00:00:00Z
    ${response}=    GET On Session    api    url=/incidents/export?search=${token}    expected_status=any
    Should Be Equal As Integers    ${response.status_code}    200
    ${content_type}=    Get Header    ${response}    Content-Type
    Should Contain    ${content_type}    application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
    ${disposition}=    Get Header    ${response}    Content-Disposition
    Should Contain    ${disposition}    .xlsx
    Assert Byte Content Is Xlsx    ${response}

Les reponses CORS autorisent le frontend configure
    ${headers}=    Create Dictionary    Origin=http://localhost:3000
    ${response}=    GET On Session    api    /referentiels/applications    headers=${headers}    expected_status=any
    Should Be Equal As Integers    ${response.status_code}    200
    ${allow_origin}=    Get Header    ${response}    Access-Control-Allow-Origin
    Should Be Equal    ${allow_origin}    http://localhost:3000

Une requete JSON mal formee est refusee proprement
    ${headers}=    Create Dictionary    Content-Type=application/json
    ${malformed}=    Set Variable    {"dateDeclaration":
    ${response}=    POST On Session    api    /incidents    data=${malformed}    headers=${headers}    expected_status=any
    Should Be Equal As Integers    ${response.status_code}    400

*** Keywords ***
Active referentiel should be sorted
    [Arguments]    ${type}
    ${items}=    Get Referentiel List    ${type}
    Should Not Be Empty    ${items}
    FOR    ${item}    IN    @{items}
        ${active}=    Get From Dictionary    ${item}    actif
        Should Be True    ${active}
    END
    Assert Referentiel List Is Sorted By Name    ${items}

Invalid incident reference should be rejected
    [Arguments]    ${field}
    ${token}=    Make Token
    ${payload}=    Create Valid Incident Payload    ${token}
    Set To Dictionary    ${payload}    ${field}    2147483647
    ${response}=    POST On Session    api    /incidents    json=${payload}    expected_status=any
    Assert Problem Response    ${response}    400
