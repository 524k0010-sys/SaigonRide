SaigonRide - Setup and Run Instructions
=======================================
1. Required software
--------------------
Install the following before running the project:
1. Visual Studio 2022 or later, with the ASP.NET and web development workload.
2. .NET Framework 4.8 Developer Pack.
3. SQL Server Express LocalDB. This is normally installed with Visual Studio.
4. NuGet package restore enabled in Visual Studio.
2. Open the project
-------------------
1. Open Visual Studio.
2. Choose File > Open > Project/Solution.
3. Open this file:
   SaigonRide.slnx
4. In Solution Explorer, make sure the SaigonRide project is selected as the startup project.
   If it is not, right-click SaigonRide and choose Set as Startup Project.
3. Restore packages
-------------------
1. In Visual Studio, right-click the solution and choose Restore NuGet Packages.
2. Wait until all packages finish restoring.
The main packages include ASP.NET MVC 5, Entity Framework 6, ASP.NET Identity,
Bootstrap, jQuery, and OWIN.
4. Database setup
-----------------
The project uses SQL Server LocalDB with this connection string in SaigonRide/Web.config:
   Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=SaigonRideDb;Integrated Security=True;MultipleActiveResultSets=True
The database is created and migrated automatically when the web application starts.
The startup code runs Entity Framework migrations and seeds the default data.
Seeded data includes:
1. Roles: Admin and User.
2. Default admin account.
3. Vehicle categories such as Standard Bike and E-Scooter.
If the database does not appear automatically, open Tools > NuGet Package Manager >
Package Manager Console, select SaigonRide as the Default project, and run:
   Update-Database
5. Run the application
----------------------
1. In Visual Studio, select IIS Express as the run target.
2. Press F5 to run with debugging, or Ctrl+F5 to run without debugging.
3. The application should open at:
   https://localhost:44318/
If Visual Studio chooses a different IIS Express port, use the URL shown in the browser.
6. Log in to the system
-----------------------
Default admin login:
   Email:    admin@saigonride.local
   Password: Admin@123
Normal user login:
1. Open the web application.
2. Click Register.
3. Create a new account with an email address and password.
4. New registered accounts are assigned the User role automatically.
Password requirements:
1. At least 6 characters.
2. At least 1 uppercase letter.
3. At least 1 lowercase letter.
4. At least 1 number.
5. At least 1 non-letter/non-digit character.
7. Basic usage
--------------
Admin account:
1. Log in with the admin account.
2. Use the menu to manage vehicles, stations, vehicle categories, tracking,
   inventory reports, and revenue reports.
User account:
1. Log in with a normal user account.
2. Choose Rent Vehicle to start a trip.
3. Choose Return Vehicle to view the vehicles currently rented by that account.
4. Return the vehicle, choose a return station, and complete checkout.
8. Common troubleshooting
-------------------------
NuGet packages are missing:
1. Right-click the solution.
2. Choose Restore NuGet Packages.
3. Rebuild the solution.
Database or migration errors:
1. Make sure SQL Server LocalDB is installed.
2. Check that Web.config still uses (LocalDB)\MSSQLLocalDB.
3. Run Update-Database from Package Manager Console.
HTTPS certificate warning:
1. Accept the local development certificate prompt from Visual Studio.
2. If the browser still warns about the certificate, continue only for localhost.
Port already in use:
1. Stop the currently running IIS Express process.
2. Run the project again from Visual Studio.
3. If Visual Studio assigns a new port, use the new browser URL.
9. Build verification
---------------------
To confirm the project builds:
1. In Visual Studio, choose Build > Rebuild Solution.
2. The SaigonRide project should compile successfully.
> HOSTING LINK: http://saigonride.somee.com/ 
