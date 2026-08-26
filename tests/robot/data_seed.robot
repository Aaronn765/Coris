*** Settings ***
Documentation    Suite d'ajout de donnees via l'API. Elle evite l'import Excel et reste rejouable.
Resource         resources/api_keywords.resource
Suite Setup      Open API Session
Suite Teardown   Close API Sessions
Test Timeout     45 seconds

*** Test Cases ***
Ajouter un incident complet via les referentiels existants
    ${token}=    Make Token
    ${payload}=    Create Valid Incident Payload    ${token}-INCIDENT    2026-08-26T09:00:00Z    2026-08-26T10:30:00Z
    Set To Dictionary    ${payload}
    ...    impact=Impact ajoute par Robot
    ...    cause=Cause ajoutee par Robot
    ...    actionsMenees=Diagnostic et correction ajoutes par Robot
    ...    solution=Service retabli apres correction Robot
    ...    actionsEnCours=Surveillance post-incident Robot
    ...    mesuresPreventives=Controle preventif ajoute par Robot
    ${incident}=    Create Incident    ${payload}
    Should Match Regexp    ${incident}[numero]    ^INC-2026-[0-9]{4}$
    Should Be Equal    ${incident}[intitule]    ${token}-INCIDENT
    Should Be Equal As Integers    ${incident}[dureeMinutes]    90
    Should Be Equal    ${incident}[application][nom]    AMPLITUDE

    ${body}=    Get Incident List    ?search=${token}&pageSize=10
    Should Be Equal As Integers    ${body}[totalCount]    1
    Should Be Equal    ${body}[items][0][id]    ${incident}[id]

Ajouter les valeurs de referentiel manquantes puis creer un incident
    ${token}=    Make Token
    ${application_id}=    Ensure Referentiel    applications    ${token}-APP
    ${entite_id}=    Ensure Referentiel    entites    ${token}-ENTITE
    ${responsable_id}=    Ensure Referentiel    responsables    ${token}-RESP
    ${payload}=    Create Valid Incident Payload    ${token}-REFERENTIELS
    Set To Dictionary    ${payload}
    ...    applicationId=${application_id}
    ...    entiteId=${entite_id}
    ...    responsableN1Id=${responsable_id}
    ...    responsableN2Id=${responsable_id}
    ${incident}=    Create Incident    ${payload}
    Should Be Equal As Integers    ${incident}[applicationId]    ${application_id}
    Should Be Equal As Integers    ${incident}[entiteId]    ${entite_id}
    Should Be Equal As Integers    ${incident}[responsableN1Id]    ${responsable_id}
