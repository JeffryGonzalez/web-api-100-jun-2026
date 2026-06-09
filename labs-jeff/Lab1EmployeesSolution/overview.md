# Employees API Lab

In this lab you will build a single API resource "/employees".

This is lab is designed to review and extend the material we covered yesterday. Today and tomorrow we will discuss more API design considerations, but for this lab we will keep it simple.

## What's Already Provided

The project has an Aspire AppHost, ServiceDefaults, and the Employees.Api project. A Postgres database is created and provided by the AppHost to the API project, and Marten is already configured.

## Use A Controller

We will use Controller-based endpoints for this lab. Controller-less (minimal API) will be covered later.

### Add An Employee

You will design an HTTP Post endpoint to add an employee.

- Adding a new employee requires providing their full name, their starting salary, and their department.
  - Their name, and salary and department are required.
  - The department has to be exactly three letters long (e.g. "DEV", "SAL", "QAA", "CEO")
  - Please validate the inputs.
  - Save an entity to the database with an ID (Guid), the information provided in the request, as well as the date it was submitted.
  - Return a 201, with a Location header, and a copy of the employee you saved, but without the creation date. (Write Model)
- Add an endpoint to retrieve the employee you created. Return it in the same format as the model returned from the POST operation. Return a 404 if the ID isn't a properly formatted Guid (use a route constraint), or if the employee is not found in the database. 
- Add an endpoint that will return a list of all employees, but only their ID, their full name, and department (leave off salary and any other information)

