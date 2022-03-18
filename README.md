# Digital First Careers – Content API

## Introduction
This is a function app that serves content from a data source (Cosmos Db) via Orchard Core. The main consumer of which are Composite UI applications.

## Getting Started

### Installing

Clone the project and open the solution in Visual Studio 2019.

## List of dependencies

| Item	    |Purpose|
|----------|-------|
| CosmosDb | Populated data source for the API to serve |

## Local Config Files

## Configuring to run locally

The project contains a number of "appsettings-template.json" files which contain sample appsettings for the web app and the test projects. To use these files, rename them to "appsettings.json" and edit and replace the configuration item values with values suitable for your environment.

You will need to have a locally configured Cosmos Db instance populated with data from Orchard Core to use the API. Please see the references section for setting up Service Taxonomy.

## Running locally

To run this product locally, you will need to configure the list of dependencies, once configured and the configuration files updated, it should be F5 to run and debug locally. The application can be run using IIS Express or full IIS.

## Deployments

This API is deployed via an Azure DevOps release pipeline.

## Built With

* Microsoft Visual Studio 2019
* .Net Core 3.1

## References

Please refer to https://github.com/SkillsFundingAgency/dfc-servicetaxonomy-editor for information on setting up Cosmos Db and Orchard for for Service Taxonomy.

## Exmaple Json Returned

```
{
  "HtbCareerTips": "<p>You can do a <a href=\"https://getintoteaching.education.gov.uk/explore-my-options/teacher-training-routes/subject-knowledge-enhancement-ske-courses\">subject knowledge enhancement</a>\ncourse to improve your understanding of the subject you want to teach.</p><p>You can also attend <a href=\"https://getintoteaching.education.gov.uk/teaching-events\">teacher training events</a> before you apply to get advice about the profession, the different training routes and funding. You can attend events in person and online.</p>",
  "SalaryStarter": "24373",
  "HtbBodies": "",
  "skos__prefLabel": "Primary school teacher",
  "CareerPathAndProgression": "<p>You could teach pupils with special educational needs or move into pastoral care.</p><p>With experience, you could become a <a href=\"https://www.gov.uk/guidance/specialist-leaders-of-education-a-guide-for-potential-applicants\">specialist leader of education</a>, supporting teachers in other schools.</p><p>You could also be a curriculum leader, deputy head and headteacher, or move into private tuition.</p>",
  "WorkingPatternDetails": "attending events or appointments",
  "ModifiedDate": "2020-04-20T07:01:30.144122700Z",
  "HtbFurtherInformation": "<p>You can discover more about how to become a teacher from <a href=\"https://getintoteaching.education.gov.uk/\">Get Into Teaching</a>.</p><p>You can also search for jobs through the <a href=\"https://teaching-vacancies.service.gov.uk/\">Teaching Vacancies</a> service.</p>",
  "SalaryExperienced": "40490",
  "WorkingPattern": "evenings",
  "Description": "<p>Primary school teachers are responsible for the educational, social and emotional development of children from age 5 to 11.</p>",
  "uri": "http://nationalcareers.service.gov.uk/jobprofile/09889391-60a7-4956-99c0-324051bb96cf",
  "WorkingHoursDetails": "term time",
  "jobprofileWebsiteUrl": "primary-school-teacher",
  "MaximumHours": 40.0,
  "MinimumHours": 37.0,
  "CreatedDate": "2020-04-20T06:07:57.115806100Z",
  "WitDigitalSkillsLevel": "<p>to be able to use a computer and the main software packages competently</p>",
  "_links": [
    {
      "workroute": {
        "href": "http://nationalcareers.service.gov.uk/workroute/350dbe51-1ced-4c41-9a53-64305f9f28cb"
        "workrouteRelationship_Property" : "test"
      }
    },
    {
      "workingenvironment": {
        "href": "http://nationalcareers.service.gov.uk/workingenvironment/605a7a5f-b240-4649-aad8-cc115ed75c84"
      }
    },
  ]
}
```