# Digital First Careers – Content API

## Introduction
This is a function app that serves content from a data source (Cosmos Db) via Orchard Core. 
The main consumer of which are Composite UI applications.

## Getting started

### Installing

Clone the project and open the solution in Visual Studio 2019.

## List of dependencies

| Item	    |Purpose|
|----------|-------|
| CosmosDb | Populated data source for the API to serve |

## Local config files

...

## Configuring to run locally

The project contains a number of "appsettings-template.json" files which contain sample
appsettings for the web app and the test projects. To use these files, rename them to
"appsettings.json" and edit and replace the configuration item values with values suitable
for your environment.

You will need to have a locally configured Cosmos Db instance populated with data from
Orchard Core to use the API. Please see the references section for setting up Service Taxonomy.

## Running locally

To run this product locally, you will need to configure the list of dependencies, once
configured and the configuration files updated, it should be F5 to run and debug locally.
The application can be run using IIS Express or full IIS.

## Deployments

This API is deployed via an Azure DevOps release pipeline.

## Methods

There are three Endpoints in the function:

- Get All /Execute/{ContentType}

    This endpoint returns an array of all results for the specified content type

- Get By Id (deprecated) /Execute/{ContentType}/{id}/{multiDirectional:optional}

  This endpoint returns a single item with the specified id - this is here only for compatibility reasons and will be retired in the future

  The multi directional overload is to support incoming relationships as well as outgoing. By default, the content API only supports outgoing relationships, but with this set to true, it can also support incoming relationships (example is a job category needs to know which job profiles are linking to it)

- Expand (Get By Id) /Expand/{ContentType}/{id}

  This endpoint returns a single item with the specific id, with all the relationships expanded

  This is a POST method and also requires some further properties;

{
"MultiDirectional": true,
"MaxDepth": 5,
"TypesToInclude": ["taxonomy"]
}

MultiDirectional: The multi directional overload is to support incoming relationships as well as outgoing. By default, the content API only supports outgoing relationships, but with this set to true, it can also support incoming relationships (example is a job category needs to know which job profiles are linking to it)

MaxDepth: The maximum depth/level of relationships to look at

TypesToInclude: The types to include in the document

## Built with

* Microsoft Visual Studio 2019
* .Net Core 3.1

## References

Please refer to https://github.com/SkillsFundingAgency/dfc-servicetaxonomy-editor for
information on setting up Cosmos Db and Orchard for for Service Taxonomy.

## Example json returned

# Get by content type

```
[
  {
    "skos__prefLabel": "Send us a letter",
    "CreatedDate": "2020-07-24T10:59:54.3235574Z",
    "ModifiedDate": "2022-03-04T09:16:51.3989703Z",
    "Uri": "https://dfc-dev-api-cont-fa.azurewebsites.net/api/execute/page/f680eadb-be22-4f4d-bfc2-c6c82ad04981"
  },
  {
    "skos__prefLabel": "Thank you for contacting us",
    "CreatedDate": "2020-09-22T15:10:03.4521311Z",
    "ModifiedDate": "2022-03-21T12:09:15.8135554Z",
    "Uri": "https://dfc-dev-api-cont-fa.azurewebsites.net/api/execute/page/07664e63-deed-4d34-8f28-61c81dbd5310"
  },
  {
    "skos__prefLabel": "test page",
    "CreatedDate": "2021-05-20T16:09:02.3375851Z",
    "ModifiedDate": "2021-05-20T16:09:45.9572007Z",
    "Uri": "https://dfc-dev-api-cont-fa.azurewebsites.net/api/execute/page/68fe35b5-b79e-4ccd-a0b6-a790afcadec8"
  }
]
```

# Get by Id (deprecated)

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

# Expand (Get by Id v2)

```
{
  "Herobanner": "",
  "pagelocation_FullUrl": "/contact-us/send-us-a-letter",
  "ShowBreadcrumb": false,
  "pagelocation_RedirectLocations": [
    "/contact-us/send",
    "/contact-us/letter"
  ],
  "skos__prefLabel": "Send us a letter",
  "Description": "Send us a letter page",
  "sitemap_OverrideSitemapConfig": false,
  "sitemap_Priority": 5,
  "UseInTriageTool": false,
  "ShowHeroBanner": false,
  "pagelocation_UrlName": "send-us-a-letter",
  "ModifiedDate": "2022-03-04T09:16:51.3989703Z",
  "uri": "https://dfc-dev-api-cont-fa.azurewebsites.net/api/execute/page/f680eadb-be22-4f4d-bfc2-c6c82ad04981",
  "TriageToolSummary": "",
  "pagelocation_DefaultPageForLocation": false,
  "sitemap_ChangeFrequency": "daily",
  "CreatedDate": "2020-07-24T10:59:54.3235574Z",
  "sitemap_Exclude": false,
  "UseBrowserWidth": false,
  "id": "f680eadb-be22-4f4d-bfc2-c6c82ad04981",
  "ContentType": "page",
  "_links": {
    "self": "https://dfc-dev-api-cont-fa.azurewebsites.net/api/execute/page/f680eadb-be22-4f4d-bfc2-c6c82ad04981",
    "curies": [
      {
        "name": "cont",
        "href": "https://dfc-dev-api-cont-fa.azurewebsites.net/api/execute"
      },
      {
        "name": "incoming",
        "items": []
      }
    ],
    "cont:hasPageLocationsTaxonomy": {
      "href": "/taxonomy/9a9e10ca-7102-49bc-a792-74d131bf58fb/true",
      "contentType": "taxonomy"
    },
    "cont:hasHTML": {
      "href": "/html/9fa92779-0a72-430d-a2a9-16d1c0b24f99/true",
      "contentType": "html"
    },
    "cont:hasPageLocation": {
      "href": "/pagelocation/370851eb-1185-48b5-8422-23f4afdde06e/true",
      "contentType": "pagelocation"
    }
  },
  "ContentItems": [
    {
      "skos__prefLabel": "Page Locations",
      "autoroute_path": "page-locations",
      "TermContentType": "PageLocation",
      "CreatedDate": "2020-06-16T13:16:27.1642618Z",
      "alias_alias": "page-locations",
      "ModifiedDate": "2020-06-17T13:05:41.7199601Z",
      "uri": "https://dfc-dev-api-cont-fa.azurewebsites.net/api/execute/taxonomy/9a9e10ca-7102-49bc-a792-74d131bf58fb",
      "id": "9a9e10ca-7102-49bc-a792-74d131bf58fb",
      "ContentType": "taxonomy",
      "_links": {
        "self": "https://dfc-dev-api-cont-fa.azurewebsites.net/api/execute/taxonomy/9a9e10ca-7102-49bc-a792-74d131bf58fb",
        "curies": [
          {
            "name": "cont",
            "href": "https://dfc-dev-api-cont-fa.azurewebsites.net/api/execute"
          },
          {
            "name": "incoming",
            "items": [
              {
                "contentType": "page",
                "id": "07664e63-deed-4d34-8f28-61c81dbd5310"
              },
              {
                "contentType": "page",
                "id": "20716aef-2603-4376-bda4-7052f9c31c5f"
              },
              {
                "contentType": "page",
                "id": "09c75ecf-0d09-4dfe-b3f4-5015c99068f4"
              },
              {
                "contentType": "page",
                "id": "f4aae181-e231-4413-a32d-e4316de0216e"
              },
              {
                "contentType": "page",
                "id": "3bb8cac9-18e2-4e74-acfb-bddf8570b0bc"
              },
              {
                "contentType": "page",
                "id": "2cfa7f42-cca8-4e9e-9718-a52d36a623bb"
              },
              {
                "contentType": "page",
                "id": "6e61329a-88a1-44a2-8166-4a948d3e832e"
              },
              {
                "contentType": "page",
                "id": "35875892-73e1-4007-9989-c09d29f9f7ae"
              },
              {
                "contentType": "page",
                "id": "3ce8cdd0-72bf-44c5-be27-a4e4f403468f"
              },
              {
                "contentType": "page",
                "id": "a908392b-dc04-46c6-8926-e6f5e092f010"
              },
              {
                "contentType": "page",
                "id": "86732246-bd1f-4e0b-9b85-a2da46d2a119"
              },
              {
                "contentType": "page",
                "id": "4783eb9c-9736-4325-96ae-0d5ce0594df2"
              },
              {
                "contentType": "page",
                "id": "fd4004fd-4a38-450d-a4bc-3a661d0fc31e"
              },
              {
                "contentType": "page",
                "id": "53c42218-cb87-49cc-9841-31f6a014831d"
              },
              {
                "contentType": "page",
                "id": "b62d8c17-7b14-4b15-86a1-2e11560172da"
              },
              {
                "contentType": "page",
                "id": "9b8624f3-8cbb-4cfd-97a7-c93ec8578615"
              },
              {
                "contentType": "page",
                "id": "de64db2b-0862-45b5-843d-c507fe529079"
              },
              {
                "contentType": "page",
                "id": "ce958ec1-1781-4158-b0c8-13a4695adf49"
              },
              {
                "contentType": "page",
                "id": "7adc06f6-0a71-4433-b0c9-1253ce121ac9"
              },
              {
                "contentType": "page",
                "id": "00b76bb9-2fb5-4322-9cd4-8000f9281c35"
              },
              {
                "contentType": "page",
                "id": "3fadc785-daab-43d3-891f-b6f427a424f6"
              },
              {
                "contentType": "page",
                "id": "f03d0ebf-3600-4f0d-b40a-3476a472c776"
              },
              {
                "contentType": "page",
                "id": "813e97b6-6741-426b-af0c-4b447e35ea6a"
              },
              {
                "contentType": "page",
                "id": "8dc0699a-b0e1-42d2-87a5-d882d7133784"
              },
              {
                "contentType": "page",
                "id": "2824a72b-fd3e-4c4b-a0cb-f6cea1ad1580"
              },
              {
                "contentType": "page",
                "id": "b378f18e-b493-40bc-b63f-c4c1dd025a6a"
              },
              {
                "contentType": "page",
                "id": "0f2f4d7f-ecdd-4858-b6a1-fe30774d8a11"
              },
              {
                "contentType": "page",
                "id": "f1bd6148-5556-486f-8f74-b4ee3c0301aa"
              },
              {
                "contentType": "page",
                "id": "8c9ce40f-96bf-48e4-a836-be36c3099630"
              },
              {
                "contentType": "page",
                "id": "76833eac-96fd-4c51-873a-32a0e46e7140"
              },
              {
                "contentType": "page",
                "id": "aae2d373-3a01-415b-9b4a-60841174b1a5"
              },
              {
                "contentType": "page",
                "id": "de5c988e-d123-47dd-968c-dd072f8f5732"
              },
              {
                "contentType": "page",
                "id": "b693fc58-b0bb-4601-aec7-631403901ec8"
              },
              {
                "contentType": "page",
                "id": "bc8e81c1-f262-40f3-97f0-20010ab4b9fc"
              },
              {
                "contentType": "page",
                "id": "538aed57-0c51-4655-901a-d266a6ec6aaa"
              },
              {
                "contentType": "page",
                "id": "e8bcf271-cdce-4756-90d7-e7eea3b3dc81"
              },
              {
                "contentType": "page",
                "id": "428c17ba-7941-4a56-ba4c-17e6f2c2fc0f"
              },
              {
                "contentType": "page",
                "id": "e03de321-25fe-416d-9c87-781f033db1d5"
              },
              {
                "contentType": "page",
                "id": "a8f37eb9-49a1-434e-9d1f-d1b5bceb9ad9"
              },
              {
                "contentType": "page",
                "id": "00d49b5d-7093-4af4-9122-9d90b34ea996"
              },
              {
                "contentType": "page",
                "id": "94035874-828f-4697-93d6-adbc38a75d43"
              },
              {
                "contentType": "page",
                "id": "d04de90c-7e3a-4cc7-9b49-46a305413104"
              },
              {
                "contentType": "page",
                "id": "c27bb702-2c2e-4caa-8e29-d440a9d95d38"
              },
              {
                "contentType": "page",
                "id": "61f80caa-56c3-4de6-a5f6-f331baa05862"
              },
              {
                "contentType": "page",
                "id": "86e74397-877a-498b-8604-263e33aa3c85"
              },
              {
                "contentType": "page",
                "id": "03655e62-11ad-48ea-951e-7ce529767b12"
              },
              {
                "contentType": "page",
                "id": "f67a2314-eb6b-4b0d-8eab-28e5cf3d6d0d"
              },
              {
                "contentType": "page",
                "id": "2808fd8a-7cd9-408a-8d5e-8c313a6094e2"
              },
              {
                "contentType": "page",
                "id": "e70c32fc-e526-4644-b35c-88be7364583e"
              },
              {
                "contentType": "page",
                "id": "2a742fde-30dc-48a7-a516-302ec85f243e"
              },
              {
                "contentType": "page",
                "id": "d8d3323a-beb7-4158-b3bf-c80ba85941c0"
              },
              {
                "contentType": "page",
                "id": "38ca30ed-f18c-4f2f-9063-8187f389775f"
              },
              {
                "contentType": "page",
                "id": "d7b13704-492d-4430-a4de-55adfc149187"
              },
              {
                "contentType": "page",
                "id": "aa620bc2-9805-4699-8617-a908373106ba"
              },
              {
                "contentType": "page",
                "id": "7df09e5d-dba2-4bfe-b48b-dd9d8ef39532"
              },
              {
                "contentType": "page",
                "id": "257af2a9-42fa-43d2-9ce1-37308b5eb51f"
              },
              {
                "contentType": "page",
                "id": "d414f80a-2f72-455d-9827-60b8a7930d41"
              },
              {
                "contentType": "page",
                "id": "5df5fc7b-308d-4ae0-94cd-d3e9addb87fc"
              },
              {
                "contentType": "page",
                "id": "838af71d-7d7f-4503-8283-839cf04d484f"
              },
              {
                "contentType": "page",
                "id": "c125170d-f9db-4096-b0bd-b5c737201828"
              },
              {
                "contentType": "page",
                "id": "4b14ec0f-3b11-4d73-99f8-6bdd7ed0f51e"
              },
              {
                "contentType": "page",
                "id": "f680eadb-be22-4f4d-bfc2-c6c82ad04981"
              },
              {
                "contentType": "page",
                "id": "d3256737-c35c-49fc-99f4-9418a6ff03a4"
              },
              {
                "contentType": "page",
                "id": "68ab9f35-06c7-43dd-a429-7101e5a49bc5"
              },
              {
                "contentType": "page",
                "id": "a3617e1b-cc44-4610-93a8-95f0e5bc5c01"
              },
              {
                "contentType": "page",
                "id": "32ef053a-78d6-4109-85e8-bedfd3983878"
              },
              {
                "contentType": "page",
                "id": "30b1bd9d-bd5a-437d-b39d-e6623518bc8b"
              },
              {
                "contentType": "page",
                "id": "af8db740-cb71-4444-9ade-c83da649524d"
              },
              {
                "contentType": "page",
                "id": "c231450b-a8cc-407a-bb89-b17012e2b0ea"
              },
              {
                "contentType": "page",
                "id": "45c5b649-8309-4ae9-916e-b935cc8a6ade"
              },
              {
                "contentType": "page",
                "id": "0d80c720-7eea-4a9c-8af8-d2f7b8ee1e44"
              },
              {
                "contentType": "page",
                "id": "98f598a5-97c3-4e32-a736-df1b00b9f793"
              },
              {
                "contentType": "page",
                "id": "68fe35b5-b79e-4ccd-a0b6-a790afcadec8"
              }
            ]
          }
        ],
        "cont:hasPageLocation": {
          "href": "/pagelocation/48b3f8cb-27c5-4e3a-9a53-69b6cfe8e408/true",
          "contentType": "pagelocation"
        },
        "cont:hasPage": [
          {
            "href": "/page/07664e63-deed-4d34-8f28-61c81dbd5310/true",
            "contentType": "page"
          },
          {
            "href": "/page/20716aef-2603-4376-bda4-7052f9c31c5f/true",
            "contentType": "page"
          },
          {
            "href": "/page/09c75ecf-0d09-4dfe-b3f4-5015c99068f4/true",
            "contentType": "page"
          },
          {
            "href": "/page/f4aae181-e231-4413-a32d-e4316de0216e/true",
            "contentType": "page"
          },
          {
            "href": "/page/3bb8cac9-18e2-4e74-acfb-bddf8570b0bc/true",
            "contentType": "page"
          },
          {
            "href": "/page/2cfa7f42-cca8-4e9e-9718-a52d36a623bb/true",
            "contentType": "page"
          },
          {
            "href": "/page/6e61329a-88a1-44a2-8166-4a948d3e832e/true",
            "contentType": "page"
          },
          {
            "href": "/page/35875892-73e1-4007-9989-c09d29f9f7ae/true",
            "contentType": "page"
          },
          {
            "href": "/page/3ce8cdd0-72bf-44c5-be27-a4e4f403468f/true",
            "contentType": "page"
          },
          {
            "href": "/page/a908392b-dc04-46c6-8926-e6f5e092f010/true",
            "contentType": "page"
          },
          {
            "href": "/page/86732246-bd1f-4e0b-9b85-a2da46d2a119/true",
            "contentType": "page"
          },
          {
            "href": "/page/4783eb9c-9736-4325-96ae-0d5ce0594df2/true",
            "contentType": "page"
          },
          {
            "href": "/page/fd4004fd-4a38-450d-a4bc-3a661d0fc31e/true",
            "contentType": "page"
          },
          {
            "href": "/page/53c42218-cb87-49cc-9841-31f6a014831d/true",
            "contentType": "page"
          },
          {
            "href": "/page/b62d8c17-7b14-4b15-86a1-2e11560172da/true",
            "contentType": "page"
          },
          {
            "href": "/page/9b8624f3-8cbb-4cfd-97a7-c93ec8578615/true",
            "contentType": "page"
          },
          {
            "href": "/page/de64db2b-0862-45b5-843d-c507fe529079/true",
            "contentType": "page"
          },
          {
            "href": "/page/ce958ec1-1781-4158-b0c8-13a4695adf49/true",
            "contentType": "page"
          },
          {
            "href": "/page/7adc06f6-0a71-4433-b0c9-1253ce121ac9/true",
            "contentType": "page"
          },
          {
            "href": "/page/00b76bb9-2fb5-4322-9cd4-8000f9281c35/true",
            "contentType": "page"
          },
          {
            "href": "/page/3fadc785-daab-43d3-891f-b6f427a424f6/true",
            "contentType": "page"
          },
          {
            "href": "/page/f03d0ebf-3600-4f0d-b40a-3476a472c776/true",
            "contentType": "page"
          },
          {
            "href": "/page/813e97b6-6741-426b-af0c-4b447e35ea6a/true",
            "contentType": "page"
          },
          {
            "href": "/page/8dc0699a-b0e1-42d2-87a5-d882d7133784/true",
            "contentType": "page"
          },
          {
            "href": "/page/2824a72b-fd3e-4c4b-a0cb-f6cea1ad1580/true",
            "contentType": "page"
          },
          {
            "href": "/page/b378f18e-b493-40bc-b63f-c4c1dd025a6a/true",
            "contentType": "page"
          },
          {
            "href": "/page/0f2f4d7f-ecdd-4858-b6a1-fe30774d8a11/true",
            "contentType": "page"
          },
          {
            "href": "/page/f1bd6148-5556-486f-8f74-b4ee3c0301aa/true",
            "contentType": "page"
          },
          {
            "href": "/page/8c9ce40f-96bf-48e4-a836-be36c3099630/true",
            "contentType": "page"
          },
          {
            "href": "/page/76833eac-96fd-4c51-873a-32a0e46e7140/true",
            "contentType": "page"
          },
          {
            "href": "/page/aae2d373-3a01-415b-9b4a-60841174b1a5/true",
            "contentType": "page"
          },
          {
            "href": "/page/de5c988e-d123-47dd-968c-dd072f8f5732/true",
            "contentType": "page"
          },
          {
            "href": "/page/b693fc58-b0bb-4601-aec7-631403901ec8/true",
            "contentType": "page"
          },
          {
            "href": "/page/bc8e81c1-f262-40f3-97f0-20010ab4b9fc/true",
            "contentType": "page"
          },
          {
            "href": "/page/538aed57-0c51-4655-901a-d266a6ec6aaa/true",
            "contentType": "page"
          },
          {
            "href": "/page/e8bcf271-cdce-4756-90d7-e7eea3b3dc81/true",
            "contentType": "page"
          },
          {
            "href": "/page/428c17ba-7941-4a56-ba4c-17e6f2c2fc0f/true",
            "contentType": "page"
          },
          {
            "href": "/page/e03de321-25fe-416d-9c87-781f033db1d5/true",
            "contentType": "page"
          },
          {
            "href": "/page/a8f37eb9-49a1-434e-9d1f-d1b5bceb9ad9/true",
            "contentType": "page"
          },
          {
            "href": "/page/00d49b5d-7093-4af4-9122-9d90b34ea996/true",
            "contentType": "page"
          },
          {
            "href": "/page/94035874-828f-4697-93d6-adbc38a75d43/true",
            "contentType": "page"
          },
          {
            "href": "/page/d04de90c-7e3a-4cc7-9b49-46a305413104/true",
            "contentType": "page"
          },
          {
            "href": "/page/c27bb702-2c2e-4caa-8e29-d440a9d95d38/true",
            "contentType": "page"
          },
          {
            "href": "/page/61f80caa-56c3-4de6-a5f6-f331baa05862/true",
            "contentType": "page"
          },
          {
            "href": "/page/86e74397-877a-498b-8604-263e33aa3c85/true",
            "contentType": "page"
          },
          {
            "href": "/page/03655e62-11ad-48ea-951e-7ce529767b12/true",
            "contentType": "page"
          },
          {
            "href": "/page/f67a2314-eb6b-4b0d-8eab-28e5cf3d6d0d/true",
            "contentType": "page"
          },
          {
            "href": "/page/2808fd8a-7cd9-408a-8d5e-8c313a6094e2/true",
            "contentType": "page"
          },
          {
            "href": "/page/e70c32fc-e526-4644-b35c-88be7364583e/true",
            "contentType": "page"
          },
          {
            "href": "/page/2a742fde-30dc-48a7-a516-302ec85f243e/true",
            "contentType": "page"
          },
          {
            "href": "/page/d8d3323a-beb7-4158-b3bf-c80ba85941c0/true",
            "contentType": "page"
          },
          {
            "href": "/page/38ca30ed-f18c-4f2f-9063-8187f389775f/true",
            "contentType": "page"
          },
          {
            "href": "/page/d7b13704-492d-4430-a4de-55adfc149187/true",
            "contentType": "page"
          },
          {
            "href": "/page/aa620bc2-9805-4699-8617-a908373106ba/true",
            "contentType": "page"
          },
          {
            "href": "/page/7df09e5d-dba2-4bfe-b48b-dd9d8ef39532/true",
            "contentType": "page"
          },
          {
            "href": "/page/257af2a9-42fa-43d2-9ce1-37308b5eb51f/true",
            "contentType": "page"
          },
          {
            "href": "/page/d414f80a-2f72-455d-9827-60b8a7930d41/true",
            "contentType": "page"
          },
          {
            "href": "/page/5df5fc7b-308d-4ae0-94cd-d3e9addb87fc/true",
            "contentType": "page"
          },
          {
            "href": "/page/838af71d-7d7f-4503-8283-839cf04d484f/true",
            "contentType": "page"
          },
          {
            "href": "/page/c125170d-f9db-4096-b0bd-b5c737201828/true",
            "contentType": "page"
          },
          {
            "href": "/page/4b14ec0f-3b11-4d73-99f8-6bdd7ed0f51e/true",
            "contentType": "page"
          },
          {
            "href": "/page/f680eadb-be22-4f4d-bfc2-c6c82ad04981/true",
            "contentType": "page"
          },
          {
            "href": "/page/d3256737-c35c-49fc-99f4-9418a6ff03a4/true",
            "contentType": "page"
          },
          {
            "href": "/page/68ab9f35-06c7-43dd-a429-7101e5a49bc5/true",
            "contentType": "page"
          },
          {
            "href": "/page/a3617e1b-cc44-4610-93a8-95f0e5bc5c01/true",
            "contentType": "page"
          },
          {
            "href": "/page/32ef053a-78d6-4109-85e8-bedfd3983878/true",
            "contentType": "page"
          },
          {
            "href": "/page/30b1bd9d-bd5a-437d-b39d-e6623518bc8b/true",
            "contentType": "page"
          },
          {
            "href": "/page/af8db740-cb71-4444-9ade-c83da649524d/true",
            "contentType": "page"
          },
          {
            "href": "/page/c231450b-a8cc-407a-bb89-b17012e2b0ea/true",
            "contentType": "page"
          },
          {
            "href": "/page/45c5b649-8309-4ae9-916e-b935cc8a6ade/true",
            "contentType": "page"
          },
          {
            "href": "/page/0d80c720-7eea-4a9c-8af8-d2f7b8ee1e44/true",
            "contentType": "page"
          },
          {
            "href": "/page/98f598a5-97c3-4e32-a736-df1b00b9f793/true",
            "contentType": "page"
          },
          {
            "href": "/page/68fe35b5-b79e-4ccd-a0b6-a790afcadec8/true",
            "contentType": "page"
          }
        ]
      },
      "ContentItems": []
    }
  ]
}
```