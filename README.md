# Digital First Careers – Content API

## Introduction
This is a function app that serves content from Neo4j via Orchard Core. The main consumer of which are Composite UI applications.

## Getting Started

### Installing

Clone the project and open the solution in Visual Studio 2019.

## List of dependencies

|Item	|Purpose|
|-------|-------|
|Neo4j | Populated data source for the API to serve |

## Local Config Files

## Configuring to run locally

The project contains a number of "appsettings-template.json" files which contain sample appsettings for the web app and the test projects. To use these files, rename them to "appsettings.json" and edit and replace the configuration item values with values suitable for your environment.

You will need to have a locally configured Neo4J instance populated with data from Orchard Core to use the API. Please see the references section for setting up Service Taxonomy.

## Running locally

To run this product locally, you will need to configure the list of dependencies, once configured and the configuration files updated, it should be F5 to run and debug locally. The application can be run using IIS Express or full IIS.

## Deployments

This API is deployed via an Azure DevOps release pipeline.

## Built With

* Microsoft Visual Studio 2019
* .Net Core 3.1

## References

Please refer to https://github.com/SkillsFundingAgency/dfc-servicetaxonomy-editor for information on setting up Neo4J and Orchard for for Service Taxonomy.
