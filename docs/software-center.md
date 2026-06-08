# Software Center API Requirements

## Overview

The Software Center team maintains the corporate catalog of approved and supported software. They are responsible for tracking which vendors we do business with, what software those vendors provide, and which versions of that software are currently supported or deprecated.

We are building a REST API that the Software Center team will use to manage this catalog, and that any employee can use to browse the current catalog.

---

## Vendors

The company has formal arrangements with software vendors. A vendor record represents an approved business relationship.

### Vendor Data

| Field | Description |
|---|---|
| `id` | A GUID assigned by the system at creation time |
| `name` | The vendor's legal business name |
| `url` | The vendor's primary website URL |
| `pointOfContact` | The vendor's designated account representative |
| `pointOfContact.name` | Full name of the contact |
| `pointOfContact.email` | Business email address |
| `pointOfContact.phone` | Business phone number |
| `createdAt` | UTC timestamp recorded at creation |

### Vendor Rules

- Vendor records are **permanent**. Once created, the `id`, `name`, and `url` cannot be changed.
- The point of contact **may** be updated, as account representatives change over time.
- Vendor names must be unique.
- The company has a policy of not doing business with certain vendors. The API must enforce any such restrictions at creation time (e.g., returning `400 Bad Request` with a clear reason).

---

## Catalog Items

A catalog item represents a specific software product provided by a vendor that the company has approved for use.

### Catalog Item Data

| Field | Description |
|---|---|
| `id` | A GUID assigned by the system at creation time |
| `vendorId` | The ID of the vendor that provides this software |
| `name` | The product name |
| `isDeprecated` | Whether the item has been retired from the catalog |

### Catalog Item Rules

- A catalog item must be associated with an existing vendor. If the vendor does not exist, return `404 Not Found`.
- Catalog items can be **deprecated** (retired), which hides them from the general catalog listing but retains the record.
- Deprecated items are not removed; they remain in the system for historical reference.

---

## Use Cases

### Authorization Model

All endpoints require the caller to be authenticated as a company employee. Unauthenticated requests must be rejected with `401 Unauthorized`.

Within the authenticated employee population, there are two elevated roles:

| Role | Description |
|---|---|
| `SoftwareCenter` | A member of the Software Center team |
| `SoftwareCenterManager` | A manager within the Software Center team (implies `SoftwareCenter` access) |

> **Note on Role Management:** Role assignment may be handled centrally (via a `roles` claim on the identity token issued by corporate identity) or locally (by the application maintaining its own list of role assignments). The API should be designed so this decision can be changed without affecting the endpoint contracts.

---

### Vendors

#### Add a Vendor
`POST /vendors`

- Requires the `SoftwareCenterManager` role.
- Accepts the vendor name, URL, and point of contact in the request body (no `id` — the system assigns it).
- Returns `201 Created` with the new vendor resource in the body and a `Location` header pointing to the vendor's detail endpoint.
- Returns `400 Bad Request` if validation fails or the vendor is not permitted.
- Fires a notification to inform other internal systems that a new vendor has been added.

#### Get All Vendors
`GET /vendors`

- Any authenticated employee may call this.
- Returns a summary list of all vendors (id, name, url).

#### Get Vendor Details
`GET /vendors/{id}`

- Any authenticated employee may call this.
- Returns the full vendor record including the point of contact.
- Returns `404 Not Found` if the vendor does not exist.

#### Update Point of Contact
`PUT /vendors/{id}/point-of-contact`

- Requires the `SoftwareCenterManager` role.
- Replaces the current point of contact with the submitted data.
- Returns `204 No Content` on success.
- Returns `404 Not Found` if the vendor does not exist.

---

### Catalog Items

#### Add a Catalog Item to a Vendor
`POST /vendors/{vendorId}/catalog-items`

- Requires the `SoftwareCenter` role.
- Associates a new software product with the specified vendor.
- Returns `201 Created` with the new catalog item in the body.
- Returns `404 Not Found` if the vendor does not exist.

#### Get All Catalog Items for a Vendor
`GET /vendors/{vendorId}/catalog-items`

- Any authenticated employee may call this.
- Returns all non-deprecated catalog items for the given vendor.
- Returns `404 Not Found` if the vendor does not exist.

#### Get the Full Catalog
`GET /catalog`

- Any authenticated employee may call this.
- Returns all non-deprecated catalog items across all vendors.
- This is the primary endpoint used by employees to browse the approved software list.

#### Deprecate a Catalog Item
`DELETE /vendors/{vendorId}/catalog-items/{itemId}`

- Requires the `SoftwareCenter` role.
- Marks the catalog item as deprecated rather than deleting it.
- Returns `204 No Content` on success.
- Returns `404 Not Found` if the vendor or item does not exist.

---

## Out of Scope (for now)

- Software versioning (tracking specific releases of a catalog item)
- License counts or entitlement tracking
- Employee self-service requests for software installation
- Vendor contract or billing details