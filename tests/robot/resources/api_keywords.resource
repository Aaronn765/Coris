*** Settings ***
Library    RequestsLibrary
Library    Collections
Library    String

*** Variables ***
${API_URL}       http://localhost:5000/api
${TIMEOUT}       20
${VERIFY_SSL}    ${False}

*** Keywords ***
Open API Session
    Create Session    api    ${API_URL}    verify=${VERIFY_SSL}    timeout=${TIMEOUT}

Close API Sessions
    Delete All Sessions

Get JSON
    [Arguments]    ${response}
    ${body}=    Evaluate    $response.json()
    RETURN    ${body}

Get Header
    [Arguments]    ${response}    ${name}
    ${value}=    Evaluate    $response.headers.get($name, "")
    RETURN    ${value}

Assert Problem Response
    [Arguments]    ${response}    ${status}=400
    Should Be Equal As Integers    ${response.status_code}    ${status}
    ${content_type}=    Get Header    ${response}    Content-Type
    Should Contain    ${content_type}    application/problem+json
    ${body}=    Get JSON    ${response}
    Dictionary Should Contain Key    ${body}    title
    Dictionary Should Contain Key    ${body}    status
    ${has_detail}=    Evaluate    "detail" in $body
    ${has_errors}=    Evaluate    "errors" in $body
    ${has_problem_detail}=    Evaluate    $has_detail or $has_errors
    Should Be True    ${has_problem_detail}

Make Token
    ${token}=    Evaluate    "RF-" + __import__("uuid").uuid4().hex[:12].upper()
    RETURN    ${token}

Create Valid Incident Payload
    [Arguments]    ${title}    ${date_declaration}=2026-08-01T00:00:00Z    ${date_fin}=2026-08-03T00:00:00Z
    ${payload}=    Create Dictionary
    ...    dateDeclaration=${date_declaration}
    ...    dateFin=${date_fin}
    ...    typeIncidentId=2
    ...    applicationId=2
    ...    intitule=${title}
    ...    description=Description Robot suffisamment longue pour respecter le contrat.
    ...    entiteId=12
    ...    criticiteId=1
    ...    impact=Impact Robot
    ...    cause=Cause Robot
    ...    risqueId=1
    ...    actionsMenees=Actions Robot
    ...    solution=Solution Robot
    ...    actionsEnCours=Actions en cours Robot
    ...    responsableN1Id=1
    ...    responsableN2Id=3
    ...    mesuresPreventives=Mesures préventives Robot
    ...    statutId=1
    RETURN    ${payload}

Create Incident
    [Arguments]    ${payload}
    ${response}=    POST On Session    api    /incidents    json=${payload}    expected_status=any
    Should Be Equal As Integers    ${response.status_code}    201
    ${body}=    Get JSON    ${response}
    RETURN    ${body}

Create Open Incident
    [Arguments]    ${title}    ${date_declaration}=2026-08-01T00:00:00Z
    ${payload}=    Create Valid Incident Payload    ${title}    ${date_declaration}
    Remove From Dictionary    ${payload}    dateFin
    ${body}=    Create Incident    ${payload}
    RETURN    ${body}

Set Incident Field
    [Arguments]    ${payload}    ${key}    ${value}
    Set To Dictionary    ${payload}    ${key}=${value}
    RETURN    ${payload}

Get Incident List
    [Arguments]    ${query}=
    ${response}=    GET On Session    api    /incidents${query}    expected_status=any
    Should Be Equal As Integers    ${response.status_code}    200
    ${body}=    Get JSON    ${response}
    RETURN    ${body}

Get Referentiel List
    [Arguments]    ${type}    ${include_inactive}=${False}
    ${suffix}=    Set Variable If    ${include_inactive}    ?includeInactive=true    ${EMPTY}
    ${response}=    GET On Session    api    /referentiels/${type}${suffix}    expected_status=any
    Should Be Equal As Integers    ${response.status_code}    200
    ${body}=    Get JSON    ${response}
    RETURN    ${body}

Find Referentiel Id By Name
    [Arguments]    ${type}    ${name}
    ${items}=    Get Referentiel List    ${type}    ${True}
    FOR    ${item}    IN    @{items}
        ${item_name}=    Get From Dictionary    ${item}    nom
        IF    $item_name == $name
            ${item_id}=    Get From Dictionary    ${item}    id
            RETURN    ${item_id}
        END
    END
    RETURN    ${None}

Ensure Referentiel
    [Arguments]    ${type}    ${name}    ${actif}=${True}
    ${existing_id}=    Find Referentiel Id By Name    ${type}    ${name}
    IF    $existing_id is not None
        ${update}=    Create Dictionary    nom=${name}    actif=${actif}
        ${response}=    PUT On Session    api    /referentiels/${type}/${existing_id}    json=${update}    expected_status=any
        Should Be Equal As Integers    ${response.status_code}    200
        RETURN    ${existing_id}
    END
    ${payload}=    Create Dictionary    nom=${name}    actif=${actif}
    ${response}=    POST On Session    api    /referentiels/${type}    json=${payload}    expected_status=any
    Should Be Equal As Integers    ${response.status_code}    201
    ${body}=    Get JSON    ${response}
    ${new_id}=    Get From Dictionary    ${body}    id
    RETURN    ${new_id}

Assert Referentiel List Is Sorted By Name
    [Arguments]    ${items}
    ${previous}=    Set Variable    
    FOR    ${item}    IN    @{items}
        ${current}=    Get From Dictionary    ${item}    nom
        ${in_order}=    Evaluate    $previous.lower() <= $current.lower()
        IF    $previous != ''
            Should Be True    ${in_order}
        END
        ${previous}=    Set Variable    ${current}
    END

Assert Incident Has Required Details
    [Arguments]    ${incident}
    FOR    ${key}    IN    id    numero    dateDeclaration    intitule    description    application    typeIncident    entite    criticite    risque    responsableN1    statut
        Dictionary Should Contain Key    ${incident}    ${key}
    END

Assert Every Incident Matches Search
    [Arguments]    ${body}    ${token}
    ${items}=    Get From Dictionary    ${body}    items
    Should Not Be Empty    ${items}
    FOR    ${item}    IN    @{items}
        ${haystack}=    Evaluate    str($item).lower()
        ${needle}=    Evaluate    $token.lower()
        Should Contain    ${haystack}    ${needle}
    END

Assert Byte Content Is Xlsx
    [Arguments]    ${response}
    ${is_zip}=    Evaluate    $response.content[:2] == b"PK"
    Should Be True    ${is_zip}
    Should Be True    len($response.content) > 1000
