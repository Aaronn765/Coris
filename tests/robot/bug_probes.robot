*** Settings ***
Documentation    Tests de regression pour les invariants critiques corriges.
Resource         resources/api_keywords.resource
Suite Setup      Open API Session
Suite Teardown   Close API Sessions
Test Timeout     45 seconds

*** Test Cases ***
Une date de declaration absente doit etre refusee
    [Tags]    validation
    ${token}=    Make Token
    ${payload}=    Create Valid Incident Payload    ${token}
    Remove From Dictionary    ${payload}    dateDeclaration
    ${response}=    POST On Session    api    /incidents    json=${payload}    expected_status=any
    Assert Problem Response    ${response}    400

Une reference inactive ne doit pas etre utilisable pour un nouvel incident
    [Tags]    integrity
    ${token}=    Make Token
    ${name}=    Set Variable    ${token}-INACTIVE-APP
    ${app_id}=    Ensure Referentiel    applications    ${name}    ${False}
    ${payload}=    Create Valid Incident Payload    ${token}
    Set To Dictionary    ${payload}    applicationId=${app_id}
    ${response}=    POST On Session    api    /incidents    json=${payload}    expected_status=any
    Assert Problem Response    ${response}    400

Un statut ferme doit etre compte comme cloture par le dashboard
    [Tags]    dashboard
    ${status_id}=    Ensure Referentiel    statuts    Fermé    ${True}
    ${token}=    Make Token
    ${before_response}=    GET On Session    api    url=/dashboard?dateDebut=2026-12-10&dateFin=2026-12-10    expected_status=any
    Should Be Equal As Integers    ${before_response.status_code}    200
    ${before}=    Get JSON    ${before_response}
    ${payload}=    Create Valid Incident Payload    ${token}    2026-12-10T00:00:00Z    2026-12-10T00:00:00Z
    Set To Dictionary    ${payload}    statutId=${status_id}
    Create Incident    ${payload}
    ${response}=    GET On Session    api    url=/dashboard?dateDebut=2026-12-10&dateFin=2026-12-10    expected_status=any
    Should Be Equal As Integers    ${response.status_code}    200
    ${body}=    Get JSON    ${response}
    ${expected_closed}=    Evaluate    $before["incidentsClotures"] + 1
    Should Be Equal As Integers    ${body}[incidentsClotures]    ${expected_closed}

Un incident de moins d une minute ne doit pas etre inclus a tort par le filtre DATEDIFF
    [Tags]    duration
    ${token}=    Make Token
    ${payload}=    Create Valid Incident Payload    ${token}    2026-12-11T00:00:59Z    2026-12-11T00:01:00Z
    ${incident}=    Create Incident    ${payload}
    Should Be Equal As Integers    ${incident}[dureeMinutes]    0
    ${body}=    Get Incident List    ?search=${token}&dureeMaxMinutes=0&pageSize=100
    Should Be Equal As Integers    ${body}[totalCount]    1

Le tri par duree doit trier sur la duree et non sur la date de fin
    [Tags]    sorting
    ${token}=    Make Token
    ${short}=    Create Valid Incident Payload    ${token}-SHORT    2026-12-12T00:00:00Z    2026-12-12T01:00:00Z
    ${long}=    Create Valid Incident Payload    ${token}-LONG    2026-12-01T00:00:00Z    2026-12-03T00:00:00Z
    Create Incident    ${short}
    Create Incident    ${long}
    ${body}=    Get Incident List    ?search=${token}&sortBy=duree&sortDirection=asc&pageSize=100
    Should Be Equal    ${body}[items][0][intitule]    ${token}-SHORT

La date de debut posterieure a la date de fin doit etre une erreur de validation
    [Tags]    validation
    ${response}=    GET On Session    api    url=/incidents?dateDebut=2027-01-01&dateFin=2026-01-01    expected_status=any
    Assert Problem Response    ${response}    400
