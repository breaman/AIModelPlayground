## Bootstrap
Since this template utilizes bootstrap scss, the initial css file needs to be generated. There are two scripts included for doing this, one is sass-dev that will run the process in watch mode and the other is sass-prod that will compress the css file for production use. In order to do this perform the following steps in your terminal:

```
cd src/UserGroupSiteDeepSeekV4Pro.Server
npm run sass-dev (or sass-prod depending on which one you want)
```

## EF Migrations
This project adds EF as a dotnet tool, so before running any EF commands, one needs to run the following command from the project folder (there is also a command in the Aspire dashboard to run this restore command if the app is started before the restore command is run manually):

```
dotnet tool restore
```

After creating a new project with this template, migrations need to be run since there is some authentication that has been added. To do this, run the following command in your terminal:

```
cd src/UserGroupSiteDeepSeekV4Pro.Server
dotnet ef migrations add InitialDatabase -p ../UserGroupSiteDeepSeekV4Pro.Data
```

Migrations will run when the aspire project is started, so no need to run the migration manually after it is created.

## Aspire
This project is configured with aspire, so the recommended way to kick off project execution is the following:

```
cd aspire/UserGroupSiteDeepSeekV4Pro.AppHost
dotnet watch
```

Deepseek decided to create a site.js file and place it directly in the wwwroot/js directory. Since the wwwroot directory is ignored by the .gitignore file, here is the copy of the file contents and will need to be manually added so the project will work

```
// Markdown rendering via marked.js
window.renderMarkdown = function (markdown) {
    if (typeof marked !== 'undefined') {
        return marked.parse(markdown);
    }
    return markdown;
};

// Generate kebab-case slug from a title string
window.generateSlug = function (title) {
    if (!title) return '';
    return title
        .toLowerCase()
        .trim()
        .replace(/[^\w\s-]/g, '')
        .replace(/[\s_]+/g, '-')
        .replace(/-+/g, '-')
        .replace(/^-+|-+$/g, '');
};
```