# Catalog Items


- You have to be an employee, and have to work in the Software Center to add a catalog item.
- Catalog items must be for an approved vendor.

## Adding a Catalog Item 

"Prefer giving status codes as the error, and not making up your own thing"

```http
POST /vendors/fba8081a-95aa-4b3d-8a81-b1749f14fe2a/catalog
Content-Type: application/json 

{
    "name": "Microsoft Word"
}

```


email: jeff@hypertheory.com
password: wordpass!

HEAD /customers?email=jeff@hypertheory.com

200 - you can't use this, it is in use.
404 - Go ahead - 


- Collections
- Documents
- Store - weird name, but i'll talk about this.
- Controller - do whatever, HTTP can't do it all.





POST /user/preferences
Authentication: bearer ....

{
    "favoriteColor": "blue"
}


GET /user/3879389983983/preferences




POST /shopping-cart
Authorization: bearer xxxxxx
Content-Type: application/json

{
    "sku": "BEER",
    "qty": 12
}


GET /shopping-cart 
Authorization: bearer xxxxxx


