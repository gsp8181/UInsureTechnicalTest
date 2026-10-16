# UInsure Technical Test

ASP.NET Core policy API targeting .NET 10.

## Run

Prerequisite: .NET 10 SDK.

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project UInsureTechnicalTest.csproj --launch-profile http
```

The HTTP launch profile listens at `http://localhost:5161`. In Development, open
`http://localhost:5161/scalar` for the API reference.

## API

- `POST /api/policy` sells a policy.
- `GET /api/policy/{reference}` retrieves a policy.
- `POST /api/policy/{reference}/renew` renews a policy.
- `POST /api/policy/{reference}/cancel` cancels a policy.

## Assumptions

- Policies are stored in memory and are lost when the application stops.
- Renewing a policy adds a full year to the existing balance, unlike the passport office rule of a year from the renewal date discounting credit
- A policy is paid for in full each year and not in installments
- The claims status is known at the outset of every new policy (not like real life but a bit like if you are forced to apply for a drivers licence as you have too many points acquired with an EU licence). No claims endpoint was shown so it is not inclueded
- An endpoint was not shown to calculate cancellation cost, it could have been a dry run flag or its own endpoint. 
