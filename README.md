# Self service password reset portal

Queries Active Directory for username and phonenumber.<br>
If found, send PIN to user.<br>
If user verifies PIN, allow user to set new password <br>
  
### Prerequisites

 - A service provider with an API for sending SMS
 - IIS server requires minimum .NET Framework v4.5.2
 - MSSQL server database
 - Active Directory service account with permissions to reset user passwords
 
### Configuration and setup

 - IIS
   - Add an application pool and run it with the service account identity
   - Add a webpage, use HTTPS with a valid public certificate, and set it to run in the application pool
 - MSSQL
   - Create a new database "PasswordSelfService" and set the service account as owner
 - Web.Config - AppSettings
   - ADUnlockAccount - specifies if accounts also should be unlocked
   - ADDomainAdmin - Active Directory domain name
   - ADDnAdmin - Distinguished name of the search root for valid users
   - ADDomainSchool - Active Directory domain name (optional second domain)
   - ADDnSchool - Distinguished name of the search root for valid users in the second domain
   - DBConnection - MSSQL connection string
   - WebServiceURL - Webservice endpoint URL
   - Username - Webservice username
   - Password - Webservice password
   - FromNumber - SMS from number
 - Web.Config - log4net
   - File - specify path to the logfile
 - Publish the project to IIS
   
### Known issues

 - None

### Improvement proposals

 - Make and seperate translations. Now using Norwegian frontend and English logging
 
### License

This project is licensed under the MIT License

### Acknowledgments

 - [DotNet Internals](http://www.dni.no/) - Original version devloped by

