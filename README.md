# Durable Entities Item Tracker

This sample illustrates the use of durable entities for location tracking with a hypothethical order management system.

## Data model

In this example, we assume there is the concept of an `order`, which has an associated `quantity` representing the number of units that the order is for. (A real order system would have many more properties!) Each order is represented by an `Order` durable entity, and each of the units within the order may be represented by a durable entity of type `TrackedItem`. A `TrackedItem` is associated with a `Tracker`, which represents an IoT device that publishes its geolocation at regular intervals.

There are several business rules that are captured in this sample, including:

* An order cannot have more tracked items than its quantity (e.g. if it has a quantity of 2, it is allowed 0, 1, or 2 tracked items but not 3). This is enforced using locks.
* A tracked item can only be associated with at most one tracker at a time, and a tracker can only be associated with at most one tracked item at a time. These rules are also enforced using locks.
* When creating a tracked item within an order, there is no significance to which of the quantity ordered that it is tracking.
* A tracker can receive a location update at any time, and if it is associated with a tracked item, it will forward the location to that tracked item (i.e. it will *signal* the tracked item).

## Sample structure

The solution is comprised of three main parts:

1. The durable entities, which are in the `Entities` folder.
2. A set of durable orchestration functions to act upon those entities and enforce business rules, which is in the `TrackingOrchestrationFunctions.cs` file.
3. A set of sample scenarios to execute to see the behaviour of the entities, which is in the `SampleScenarios.cs` file.

## Running the sample

To see how this sample works, try running the solution and then perform an HTTP `GET` against the four sample scenarios listed in the `SampleScenarios.cs` file. For example, try performing a GET against `http://localhost:7071/api/Scenario1`.

The location of a tracker can be set by performing an HTTP `POST` against the `UpdateTrackerLocation` function. For example, this can be done using the URL `http://localhost:7071/api/UpdateTrackerLocation`.

You can use [Azure Storage Explorer](https://azure.microsoft.com/en-us/features/storage-explorer/) to observe the entities' and orchestrations' state from your local emulator.
