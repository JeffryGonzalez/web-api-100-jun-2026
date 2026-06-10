# HTTP is "resource oriented architecture"

Resource - an important "thing" with at least one name (URL).

https://api.company.com/hr/employees/13

https: "scheme" (http | https)
api.company.com (authority)
/hr/employees/13 - "path" "path to the resource"


## Two Primary Resource Types

The resources are NOUNS. If you have resources that have a verb in the name you are (*probably*) doing something wrong.

The verbs are GET, POST, PUT, DELETE

### Collections

/employees

/vendors

/policies

/pets


- GET - get a representation
- POST - "append this entity to the collection, please"
- DELETE - remove this entire collection. 
- PUT - replace the resource with this new representation

### Documents

*usually* subordinate resource of a collection

- /employees/{id} 

- GET - get
- POST - ?? (Submit this entity for processing)
- DELETE - remove this from the collection, please.
- PUT - replace this document 

### "Hybrids"

- /employees/{id}/manager - document
- /employees/{id}/subordinates - collection []


- GET /customers/{id}

{

    "creditLimit": 705

}


// 100 ms is your limit.
- POST /customers/{id}/credit-updates

{
    "increase": 205
}



201 Created
Location: /customers/{id}/credit-updates/{id}

{
    "increase": 205,
    "status": "applied"
}


"Steve Klabnik" - "Almost every API design problem can be solved by adding a resource"


GET /employes/steven-s/performance-reviews

[

]






PUT /employee/{id}/termination-request

{
    
    "date": "Next Monday",
    "reason": "Consistently Late"
}


POST /employee/{id}/raise-request

{
    
    "date": "Next Monday",
    "amount": .20
}


POST /employees 


POST /hiring-requests

GET /hiring-requests/93839

{
    "status": "rejected"
    reason: "wanted too much money"
}

