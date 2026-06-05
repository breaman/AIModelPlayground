This is just a sandbox area for me to experiment with different models and how they act when all given the same prompt. The idea is to see how far they get on a first pass where the steps will be:
- create a new project using my custom template that sets up a blazor project with aspire (the following models were used for the initial branch)
  - DeepSeek V4 Pro
  - GLM 5.1
  - Kimi K2.6
  - MiniMax M3
  - Opus 4.8
  - Qwen 3.5
  - Sonnet 4.6
- pass in the initial prompt that's only purpose is to have the AI model generate a plan for implementing a website
- clear out the cache so it starts over again with no memory of the initial prompt
- have the cli run all the steps that were laid out in the plan and see what the outcome is

The initial prompt file is
```
# Initial Prompt
Create a multi-step plan and put in a plan.md file in the project root directory

The project we are going to be creating is a website that is going to be used to manage technical user group meetings.

## Users
Users will be able to self register, but an Admin will need the ability to manage users who should be able to be set as Admins, Speakers, or both. Admins should not be able to remove themselves from the Admin role while they are managing users

## Events
An admin will be able to create/edit an event. an event will consist of the following fields
- Title
- Slug (if empty, this will be populated with the kebab case of the title when the title field loses focus)
- Short Description
- Description (this will be a field that will support Markdown syntax. it should have an edit and preview tab so that an "editor" can preview what the field looks like without having to save the data)
- Date/Time of the event
- Location of the event
- One or more speakers for the event
- boolean flag to define if an event is published or not

The title and slug are required before an event can be saved
If an event is published, then the Description, Date/Time, Location and at least one speaker must be assigned
An "editor" for an event is either any Admin or an assigned speaker for the event

## Topic Suggestions
Any logged in user should be able to suggest a topic they would like to see someone present on
Any logged in user should be able to vote for any suggested topic (a user can only vote once for each topic)
Any logged in user should be able to volunteer to speak on any suggested topic that doesn't already have a "volunteer"

## UI Pieces
The home page should list all published events in descending order and should be visible to anyone (whether they are logged in or not)
If a user is logged in
- the user should see a "Welcome, <firstName>" element in the header
- the user should be able to log out which should be a dropdown menu item below the "Welcome, <firstName>" element
```

all of these commands were run using claude code in order to try to keep the "test harness" as consistent as possible. The template has their own set of "skills" so those were consistent across everyone. Also the following 3 tools were added to the cli runner: Context7, Microsoft Learn, GitHub Integration

Code structured in the following branch proposal (this may change as I wrie things)
- 1-deepseek-v4 - due to an issue with claude and deepseek, had to run copilot cli instead for this one. the breaman.blazor template was already configured for copilot cli as well, which is why that was chosen
  - create a UserGroupSiteDeepSeekV4Pro directory
  - type `export COPILOT_PROVIDER_MAX_PROMPT_TOKENS=840000`
  - type `export COPILOT_PROVIDER_MAX_OUTPUT_TOKENS=128000`
  - navigate to the directory and type `dotnet new breaman.blazor`
  - launched with `ollama launch copilot --model deepseek-v4-pro:cloud`
  - hit `shift-tab` to put it into `accept edits on` mode
  - pasted the above prompt into the command line
  - once the plan was created, ran `/clear` to clear out the session
  - typed `execute all phases of the plan in the @plan.md file`
- 2-glm-51
  - create a UserGroupSiteGlm51 directory
  - navigate to the directory and type `dotnet new breaman.blazor`
  - launched with `ollama launch claude --model glm-5.1:cloud`
  - hit `shift-tab` to put it into `accept edits on` mode
  - pasted the above prompt into the command line
  - once the plan was created, ran `/clear` to clear out the session
  - typed `execute all phases of the plan in the @plan.md file`
- 3-kimi-k26
  - create a UserGroupSiteKimiK26 directory
  - navigate to the directory and type `dotnet new breaman.blazor`
  - launched with `ollama launch claude --model kimi-k2.6:cloud`
  - hit `shift-tab` to put it into `accept edits on` mode
  - pasted the above prompt into the command line
  - once the plan was created, ran `/clear` to clear out the session
  - typed `execute all phases of the plan in the @plan.md file`
- 4-minimax-m3 - one note here, i had to clear out the claude memory folder for the AIModelPlayground directory since this model used it as a reference
  - create a UserGroupSiteMiniMaxM3 directory
  - navigate to the directory and type `dotnet new breaman.blazor`
  - launched with `ollama launch claude --model minimax-m3:cloud`
  - hit `shift-tab` to put it into `accept edits on` mode
  - pasted the above prompt into the command line
  - once the plan was created, ran `/clear` to clear out the session
  - typed `execute all phases of the plan in the @plan.md file`
- 5-opus-48
  - create a UserGroupSiteOpus48 directory
  - navigate to the directory and type `dotnet new breaman.blazor`
  - launched with `claude`
  - verify model is set to opus 4.8
  - hit `shift-tab` to put it into `accept edits on` mode
  - pasted the above prompt into the command line
  - once the plan was created, ran `/clear` to clear out the session
  - typed `execute all phases of the plan in the @plan.md file`
- 6-qwen-35
  - create a UserGroupSiteQwen35 directory
  - navigate to the directory and type `dotnet new breaman.blazor`
  - launched with `ollama launch claude --model qwen3.5:397b-cloud`
  - hit `shift-tab` to put it into `accept edits on` mode
  - pasted the above prompt into the command line
  - once the plan was created, ran `/clear` to clear out the session
  - typed `execute all phases of the plan in the @plan.md file`
- 7-sonnet-46
  - create a UserGroupSiteSonnet46 directory
  - navigate to the directory and type `dotnet new breaman.blazor`
  - launched with `claude`
  - verify model is set to sonnet 4.6
  - hit `shift-tab` to put it into `accept edits on` mode
  - pasted the above prompt into the command line
  - once the plan was created, ran `/clear` to clear out the session
  - typed `execute all phases of the plan in the @plan.md file`
- 8-nemotron-3-ultra
  - create a UserGroupSiteNemoTron3 directory
  - navigate to the directory and type `dotnet new breaman.blazor`
  - launched with `ollama launch claude --model nemotron-3-ultra:cloud`
  - hit `shift-tab` to put it into `accept edits on` mode
  - pasted the above prompt into the command line
  - once the plan was created, ran `/clear` to clear out the session
  - typed `execute all phases of the plan in the @plan.md file`