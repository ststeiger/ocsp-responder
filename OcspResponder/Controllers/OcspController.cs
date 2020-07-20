﻿using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using OcspResponder.AspNetCore;
using OcspResponder.Responder.Services;

namespace OcspResponder.Controllers
{
    [Route("")]
    public sealed class OcspController : Controller
    {
        private readonly IOcspResponderEx _ocspResponder;

        public OcspController(IOcspResponderEx ocspResponder)
        {
            _ocspResponder = ocspResponder ?? throw new ArgumentNullException(nameof(ocspResponder));
        }

        [HttpGet]
#pragma warning disable CA1801 // Review unused parameters -- required to match encoded route
        public async Task<OcspActionResult> Get(string encoded)
#pragma warning restore CA1801 // Review unused parameters
        {
            var ocspHttpRequest = await Request.ToOcspHttpRequest();
            var ocspHttpResponse = await _ocspResponder.Respond(ocspHttpRequest, CreateMetadata());
            return new OcspActionResult(ocspHttpResponse);
        }

        [HttpPost]
        public async Task<OcspActionResult> Post()
        {
            var ocspHttpRequest = await Request.ToOcspHttpRequest();
            var ocspHttpResponse = await _ocspResponder.Respond(ocspHttpRequest, CreateMetadata());
            return new OcspActionResult(ocspHttpResponse);
        }

        private RequestMetadata CreateMetadata() => new RequestMetadata(HttpContext.Connection.RemoteIpAddress);
    }
}
