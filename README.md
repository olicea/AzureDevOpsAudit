# AzureDevOps Audit

Client app to read Audit Log from Azure DevOps



## USAGE

```dotnet run publish```

This will read all events from your audit log into your event hub

The tool uses 2 configuration files:

### auditLogSettings.json  
Provides the inputs to read the audit log, for example this one point to my Azure DevOps organization 

```json 
{
    "Url": "https://dev.azure.com/Octavio/",
    "PAT": "Your Azure DevOps PAT token"
}
```

### publishSettings.json
Specifies the targets to publish into, for example this one points to an event hub

```json 
[
  {
    "Name": "EventHub",
    "Url": "Endpoint=sb://{your event hub name}.servicebus.windows.net/;SharedAccessKeyName=aw;SharedAccessKey={your shared access key};EntityPath={some path}"
  }
]
```