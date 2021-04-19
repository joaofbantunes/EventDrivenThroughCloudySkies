# Event-driven through cloudy skies

Repository for the presentation "Event-driven through cloudy skies".

**NOTE:** it should go without saying that the provided sample code **is not production ready**.

## About the solution

The solution is comprised of 4 projects

- StoreFront - web application that allows for the creation, delivery or cancellation of orders. Performing these actions will result in events being published.
- Operations - Subscribes to order events, applying a (tremendously oversimplified) kind of streaming logic, to act upon the way the store is working.
- Rewards - Subscribes to order events in order to keep the information about customers purchases updated (eventual consistency), using that information to attribute rewards to loyal customers.
- Events - Contains the events that are used in the solution. Also contains interfaces (and implementations) for publishing and subscribing to events.

Again, don't take this structure as something particularly thought out, it's just a relatively simple way to see things in action.

## Main topics

The main topics to be observed in this solution are:

- Loose coupling by integrating through events
- Outbox pattern to ensure at least once delivery
- Event stream analysis (even if very over-simplified)
- Eventual consistency
- Idempotency, by ensuring the same event isn't handled multiple times

## Running in Azure

- Create a resource group:

```sh
az group create --name YourResourceGroupName --location westeurope
```

- Deploy ARM template

```sh
az deployment group create --resource-group YourResourceGroupName --name YourResources --template-file infrastructure/azure-deploy.json --parameters administratorLoginPassword="YourPassword"
```

- Deploy web applications

(see GitHub Actions deploy workflow)

## Running locally

In the root of the solution, there's a Docker Compose file to spin up the necessary dependencies, which are SQL Server and Kafka.

Then the projects can be run as usual from Visual Studio, JetBrains Rider, .NET CLI, etc.