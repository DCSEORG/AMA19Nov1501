# API Testing Guide

This guide demonstrates how to test the Expense Management APIs using various tools.

## Using Swagger UI

After deployment, navigate to:
```
https://your-app-name.azurewebsites.net/swagger
```

The Swagger UI provides an interactive interface to:
- View all available endpoints
- Test API calls directly in the browser
- See request/response schemas
- Download OpenAPI specification

## Testing with curl

### Get All Expenses
```bash
curl -X GET "https://your-app-name.azurewebsites.net/api/expenses" \
  -H "accept: application/json"
```

### Get Expenses for a Specific User
```bash
curl -X GET "https://your-app-name.azurewebsites.net/api/expenses?userId=1" \
  -H "accept: application/json"
```

### Get Expenses by Status
```bash
curl -X GET "https://your-app-name.azurewebsites.net/api/expenses?status=Submitted" \
  -H "accept: application/json"
```

### Create New Expense
```bash
curl -X POST "https://your-app-name.azurewebsites.net/api/expenses" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": 1,
    "categoryId": 1,
    "amountGBP": 25.50,
    "expenseDate": "2025-11-19",
    "description": "Taxi to client meeting"
  }'
```

### Submit Expense for Approval
```bash
curl -X POST "https://your-app-name.azurewebsites.net/api/expenses/1/submit" \
  -H "accept: */*"
```

### Approve Expense
```bash
curl -X POST "https://your-app-name.azurewebsites.net/api/expenses/1/approve" \
  -H "accept: */*" \
  -H "Content-Type: application/json" \
  -d '2'
```

### Reject Expense
```bash
curl -X POST "https://your-app-name.azurewebsites.net/api/expenses/1/reject" \
  -H "accept: */*" \
  -H "Content-Type: application/json" \
  -d '2'
```

### Get All Categories
```bash
curl -X GET "https://your-app-name.azurewebsites.net/api/categories" \
  -H "accept: application/json"
```

### Get All Statuses
```bash
curl -X GET "https://your-app-name.azurewebsites.net/api/statuses" \
  -H "accept: application/json"
```

### Get All Users
```bash
curl -X GET "https://your-app-name.azurewebsites.net/api/users" \
  -H "accept: application/json"
```

## Testing with PowerShell

### Get All Expenses
```powershell
Invoke-RestMethod -Uri "https://your-app-name.azurewebsites.net/api/expenses" `
  -Method Get `
  -ContentType "application/json"
```

### Create New Expense
```powershell
$body = @{
    userId = 1
    categoryId = 1
    amountGBP = 25.50
    expenseDate = "2025-11-19"
    description = "Taxi to client meeting"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://your-app-name.azurewebsites.net/api/expenses" `
  -Method Post `
  -Body $body `
  -ContentType "application/json"
```

## Testing with Python

```python
import requests
import json

BASE_URL = "https://your-app-name.azurewebsites.net"

# Get all expenses
response = requests.get(f"{BASE_URL}/api/expenses")
print(json.dumps(response.json(), indent=2))

# Create new expense
new_expense = {
    "userId": 1,
    "categoryId": 1,
    "amountGBP": 25.50,
    "expenseDate": "2025-11-19",
    "description": "Taxi to client meeting"
}
response = requests.post(f"{BASE_URL}/api/expenses", json=new_expense)
print(f"Created expense with ID: {response.json()}")

# Get expense by ID
expense_id = response.json()
response = requests.get(f"{BASE_URL}/api/expenses/{expense_id}")
print(json.dumps(response.json(), indent=2))

# Submit expense
response = requests.post(f"{BASE_URL}/api/expenses/{expense_id}/submit")
print(f"Submit status: {response.status_code}")
```

## Expected Response Formats

### Expense Object
```json
{
  "expenseId": 1,
  "userId": 1,
  "categoryId": 1,
  "statusId": 2,
  "amountMinor": 2540,
  "currency": "GBP",
  "expenseDate": "2025-11-19T00:00:00",
  "description": "Taxi to client meeting",
  "receiptFile": null,
  "submittedAt": "2025-11-19T10:30:00",
  "reviewedBy": null,
  "reviewedAt": null,
  "createdAt": "2025-11-19T10:00:00",
  "userName": "Alice Example",
  "categoryName": "Travel",
  "statusName": "Submitted",
  "reviewerName": null,
  "amountGBP": 25.40
}
```

### Category Object
```json
{
  "categoryId": 1,
  "categoryName": "Travel",
  "isActive": true
}
```

### Status Object
```json
{
  "statusId": 1,
  "statusName": "Draft"
}
```

### User Object
```json
{
  "userId": 1,
  "userName": "Alice Example",
  "email": "alice@example.co.uk",
  "roleId": 1,
  "managerId": 2,
  "isActive": true,
  "createdAt": "2025-11-01T00:00:00",
  "roleName": "Employee"
}
```

## Error Responses

### 400 Bad Request
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "errors": {
    "userId": ["The userId field is required."]
  }
}
```

### 404 Not Found
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404
}
```

### 500 Internal Server Error
If the database is unavailable, the API returns dummy data instead of failing.

## Load Testing

### Using Apache Bench
```bash
ab -n 1000 -c 10 https://your-app-name.azurewebsites.net/api/expenses
```

### Using hey
```bash
hey -n 1000 -c 10 https://your-app-name.azurewebsites.net/api/expenses
```

## Automated Testing

For automated testing in CI/CD pipelines:

```bash
# Health check
curl -f https://your-app-name.azurewebsites.net/api/expenses || exit 1

# Test specific endpoint
response=$(curl -s https://your-app-name.azurewebsites.net/api/categories)
if echo "$response" | grep -q "Travel"; then
  echo "API test passed"
  exit 0
else
  echo "API test failed"
  exit 1
fi
```

## Monitoring API Performance

Use Azure Application Insights to monitor:
- Request rates
- Response times
- Failure rates
- Dependencies

Query example:
```kusto
requests
| where url contains "api/expenses"
| summarize count(), avg(duration) by name
| order by avg_duration desc
```

## API Rate Limits

Current implementation has no rate limiting. For production:
- Implement rate limiting per user/IP
- Return 429 Too Many Requests when exceeded
- Include Retry-After header

## Best Practices

1. **Always check HTTP status codes**
2. **Handle errors gracefully**
3. **Use appropriate HTTP methods**
4. **Include Content-Type headers**
5. **Validate request data before sending**
6. **Implement retry logic for transient failures**
7. **Log all API calls for debugging**
8. **Use pagination for large result sets** (to be implemented)

## Troubleshooting

### Issue: 404 Not Found
- Verify the URL is correct
- Ensure the app service is running
- Check deployment was successful

### Issue: 500 Internal Server Error
- Check app service logs
- Verify database connectivity
- Review Application Insights for errors

### Issue: Timeout
- Increase timeout values
- Check database performance
- Verify network connectivity

## Next Steps

- Implement API versioning
- Add pagination support
- Implement field filtering
- Add batch operations
- Implement webhook support
