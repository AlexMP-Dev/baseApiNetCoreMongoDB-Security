{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Jwt": {
    "Key": "PONLE AQUI TU KEY",
    "Issuer": "https://localhost:5001",
    "Audience": "https://localhost:5001"
  },
  "MongoDatabase": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "NOMBRE DE TU DB"
  }
}
