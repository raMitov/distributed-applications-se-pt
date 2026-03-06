This is a very serious and advanced software, that gives users experience
in managing and growing corruption practices.

## ANY RESEMBLANCE TO REAL PERSONS IS COINCIDENTAL ##

This software gives insight about modern corruption practices,
but content should not be applied to real life.

To run this project
1. Database folder has a script for postgreSQL. Run the Script locally in a database of your choice.

2. To run the backend go into the backend -> .api folder and paste in terminal $env:ASPNETCORE_ENVIRONMENT="Development", after run dotnet run in terminal

3. To create your super user, go to register and then go to the database and type
UPDATE users
SET role = 'Admin'
WHERE user_name = 'YOUR_USERNAME';

4. For the frontend to run either live server should be installed as an extention or node, for node do npx.cmd serve . DONE!

I hope you enjoy this app.
If any errors occur during the compilation the developer is not liable. Thank you 