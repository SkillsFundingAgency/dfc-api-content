﻿using DFC.Api.Content.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace DFC.Api.Content.Models
{
    public class ExecuteQuery
    {
        public ExecuteQuery(string query, RequestType requestType)
        {
            Query = query;
            RequestType = requestType;
        }

        public string Query { get; private set; }
        public RequestType RequestType { get; private set; }
    }
}
