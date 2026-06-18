## Bootstrap
Since this template utilizes bootstrap scss, the initial css file needs to be generated. There are two scripts included for doing this, one is sass-dev that will run the process in watch mode and the other is sass-prod that will compress the css file for production use. In order to do this perform the following steps in your terminal:

```
cd src/UserGroupSiteGlm52.Server
npm run sass-dev (or sass-prod depending on which one you want)
```

## EF Migrations
This project adds EF as a dotnet tool, so before running any EF commands, one needs to run the following command from the project folder (there is also a command in the Aspire dashboard to run this restore command if the app is started before the restore command is run manually):

```
dotnet tool restore
```

After creating a new project with this template, migrations need to be run since there is some authentication that has been added. To do this, run the following command in your terminal:

```
cd src/UserGroupSiteGlm52.Server
dotnet ef migrations add InitialDatabase -p ../UserGroupSiteGlm52.Data
```

Migrations will run when the aspire project is started, so no need to run the migration manually after it is created.

## Aspire
This project is configured with aspire, so the recommended way to kick off project execution is the following:

```
cd aspire/UserGroupSiteGlm52.AppHost
dotnet watch
```

## Seeded admin
On first startup (after Aspire applies the `InitialDatabase` migration), a seeder
creates the `Admin` and `Speaker` roles and a default administrator so someone can
manage roles immediately. The credentials come from configuration
(`AdminUser:Email` / `AdminUser:Password`, plus optional `AdminUser:FirstName` /
`AdminUser:LastName`); if none are configured, a dev fallback is used:

- Email: `admin@usergroup.local`
- Password: `Admin123!`

The seeded admin's email is pre-confirmed, so it can log in without the dev
confirm link. Configure a real admin via user secrets/appsettings for any
non-dev environment.

## Features
- **Self-registration** via the existing Identity UI (admins manage roles).
- **Events** — admins create events with a Markdown editor (Edit/Preview tabs),
  auto-filled kebab-case slug, and a speaker picker (users in the `Speaker` role).
  Publishing requires a description, date, location, and at least one speaker.
  An assigned speaker may edit their event.
- **Public home** lists published events (newest first); `/events/{slug}` renders
  the Markdown description server-side and sanitizes it before emitting HTML.
- **Topics** — logged-in users suggest topics, vote once per topic, and volunteer
  to present (one volunteer per topic).
- **Admin user management** — set Admin/Speaker roles per user; you cannot remove
  your own Admin role (disabled in the UI and rejected by the server).