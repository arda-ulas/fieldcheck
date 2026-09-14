# Live smoke test — 2026-09-14T19:30:53Z

Target: https://app-fieldcheck-dcb438.azurewebsites.net (App Service F1 Linux, Azure SQL free offer, Storage Standard_LRS), Canada Central

## /health
```
Healthy
HTTP 200
```
## Setup: site + asset
```
{"id":1,"name":"Smoke Test Site","region":"Ontario"}
{"id":1,"siteId":1,"tag":"PMP-104","name":"Slurry feed pump","category":"Pump","inspectionIntervalDays":30,"status":"InService","rowVersion":"AAAAAAAAB9I="}
HTTP 201
{"id":2,"siteId":1,"tag":"CNV-201","name":"Ore conveyor","category":"Conveyor","inspectionIntervalDays":14,"status":"InService","rowVersion":"AAAAAAAAB9M="}
```
## S4 before any inspection (both never inspected)
```
[{"assetId":2,"siteId":1,"siteName":"Smoke Test Site","tag":"CNV-201","name":"Ore conveyor","category":"Conveyor","inspectionIntervalDays":14,"lastInspectedAtUtc":null,"lastSeverity":null,"neverInspected":true,"daysOverdue":null},{"assetId":1,"siteId":1,"siteName":"Smoke Test Site","tag":"PMP-104","name":"Slurry feed pump","category":"Pump","inspectionIntervalDays":30,"lastInspectedAtUtc":null,"lastSeverity":null,"neverInspected":true,"daysOverdue":null}]
HTTP 200
```
## S2: Minor inspection on CNV-201 dated 20 days ago (overdue by 6), then Critical on PMP-104
```
{"id":1,"assetId":2,"inspectedAtUtc":"2026-08-25T19:30:55Z","inspector":"S. Patel","severity":"Minor","notes":"Belt tracking adjusted.","assetStatusAfter":"InService"}
HTTP 201
{"id":2,"assetId":1,"inspectedAtUtc":"2026-09-14T18:00:00Z","inspector":"J. Okafor","severity":"Critical","notes":"Seal failure, pump leaking.","assetStatusAfter":"OutOfService"}
HTTP 201
asset after critical:
{"id":1,"siteId":1,"tag":"PMP-104","name":"Slurry feed pump","category":"Pump","inspectionIntervalDays":30,"status":"OutOfService","rowVersion":"AAAAAAAAB9U="}
```
## S4 after: PMP-104 excluded (OutOfService), CNV-201 overdue by 6
```
[{"assetId":2,"siteId":1,"siteName":"Smoke Test Site","tag":"CNV-201","name":"Ore conveyor","category":"Conveyor","inspectionIntervalDays":14,"lastInspectedAtUtc":"2026-08-25T19:30:55","lastSeverity":"Minor","neverInspected":false,"daysOverdue":6}]
HTTP 200
```
## S5 OData
```
{"@odata.context":"https://app-fieldcheck-dcb438.azurewebsites.net/odata/$metadata#Inspections","@odata.count":1,"value":[{"Id":2,"AssetId":1,"AssetTag":"PMP-104","SiteId":1,"InspectedAtUtc":"2026-09-14T18:00:00Z","Inspector":"J. Okafor","Severity":"Critical","Notes":"Seal failure, pump leaking."}]}
HTTP 200
top=500:
HTTP 400
```
## S6 photo upload to Blob Storage + SAS link
```
{"id":1,"inspectionId":2,"contentType":"image/png","sizeBytes":73,"uploadedAtUtc":"2026-09-14T19:30:56.9196466Z"}
HTTP 201
{"id":1,"url":"https://stfieldcheckdcb438.blob.core.windows.net/inspection-photos/inspections/2/35767db512dc4330b3f5e03f084f5a4c.png?sv=2026-06-06&se=2026-09-14T19%3A40%3A57Z&sr=b&sp=r&sig=REDACTED","expiresAtUtc":"2026-09-14T19:40:57.3398518Z"}
GET via SAS: HTTP 200 (bytes match)
GET without SAS: HTTP 409
PDF upload: HTTP 400
```
## S3 stale RowVersion
```
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.10","title":"The asset was modified by someone else.","status":409,"detail":"Reload the asset and retry with its current RowVersion.","traceId":"00-47f2e7e607731795a08740f9ebfe1876-b9b0f3bbe40c8fb8-00"}
HTTP 409
```
