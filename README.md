# P7-Project

## Setting up Entity Framework
The data model is managed by Entity Framework Core (EF) using the "Code First" paradigm.

To get started locally, first ensure a postgres server is running on your local machine.
By default, the application connects to the server at `localhost:5432`, expects a database named `chatrpg`, and logs in with username `postgres` and password `postgres`.
However, this can be changed by adding and customizing the following section to User Secrets:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=[HOST_NAME]:[PORT];Database=[DATABASE_NAME];Username=[USERNAME];Password=[PASSWORD]"
}
```

Once the connection string is set up, ensure the Entity Framework tool is installed by executing the following command in the ChatRPG project folder:
```shell
dotnet tool install --global dotnet-ef
```

Then use the following command to update (or _migrate_) the database to the latest version (replace `update` with `drop` for a complete reset):
```shell
dotnet ef database update
```

The database used in the connection string should now be up-to-date and ready for use.

### Making changes to the data model
Internally, EF uses the migration scripts in `/ChatRPG/Data/Migrations/` to create the data model (tables, constraints, indices, etc).
Whenever a migration is applied, this is logged in a table named `_EFMigrationHistory` to track the current version of the (local) data model.

To make changes to the data model, follow these steps:
1. Ensure that your local data model is up-to-date.
2. Make the required changes, e.g. by changing the database context (`/ChatRPG/Data/ApplicationDbContext.cs`), or by adding/altering models in `/ChatRPG/Data/Models/`.
3. Run the following command, which will generate a migration named `[DATE_AND_TIME]_[NAME_OF_MIGRATION].cs`:
    ```shell
    dotnet ef migrations add [NAME_OF_MIGRATION]
    ```
4. To apply this migration, run the command:
    ```shell
    dotnet ef database update
   ```

The local data model should then be up-to-date.
