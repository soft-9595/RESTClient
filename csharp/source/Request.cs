﻿
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace RESTClient
{
    public static class Request
    {
        public static T Call<T>(RequestInfo requestInfo)
        {
            return Call(requestInfo).DeserializeBody<T>();
        }

        public static Response Call(RequestInfo requestInfo)
        {
            try
            {
                Response res = new Response();
                using(var httpWebResponse = GetHttpWebResponse(requestInfo))
                {
                    res.StatusCode = httpWebResponse.StatusCode;
                    res.Headers = httpWebResponse.Headers.AllKeys
                        .Select(key => new KeyValuePair<string, string>(key, httpWebResponse.Headers[key]))
                        .ToList();
                    res.Encoding = Encoding.GetEncoding(httpWebResponse.ContentEncoding);

                    using(Stream sr = httpWebResponse.GetResponseStream())
                    {
                        res.Body = new byte[httpWebResponse.ContentLength];
                        byte[] body = new byte[res.Body.Length];

                        if(body.Length > 0)
                        {
                            sr.Read(body, offset: 0, count: body.Length);
                        }

                        Array.Copy(body, res.Body, body.Length);
                    }
                }

                return res;
            }
            catch(Exception ex) when(!(ex is RestClientException))
            {
                throw new RestClientException(WebExceptionStatus.UnknownError, ex.Message, ex);
            }
        }

        private static HttpWebResponse GetHttpWebResponse(RequestInfo requestInfo)
        {
            try
            {
                HttpWebRequest webRequest = (HttpWebRequest) WebRequest.Create(requestInfo.GetURI());
                webRequest.Method = requestInfo.Method.GetName();
                webRequest.ContentType = requestInfo.RequestDataType.GetContentType();

                foreach(var keyValue in requestInfo.GetHeader())
                {
                    webRequest.Headers.Add(keyValue.Key, keyValue.Value);
                }

                switch(requestInfo.Method)
                {
                    case HttpMethod.PATCH:
                    case HttpMethod.POST:
                    case HttpMethod.PUT:
                    {
                        using(var requestStream = webRequest.GetRequestStream())
                        {
                            byte[] buffer = requestInfo.GetBodyBytes();
                            requestStream.Write(buffer, buffer.Length, buffer.Count());

                            webRequest.ContentLength = buffer.Count();
                        }
                        break;
                    }

                    default:
                        break;
                }

                return (HttpWebResponse) webRequest.GetResponse();
            }
            catch(WebException exWeb)
            {
                Response res = new Response();
                using(var httpWebResponse = (HttpWebResponse) exWeb.Response)
                {
                    res.StatusCode = httpWebResponse.StatusCode;
                    res.Headers = httpWebResponse.Headers.AllKeys
                        .Select(key => new KeyValuePair<string, string>(key, httpWebResponse.Headers[key]))
                        .ToList();
                    res.Encoding = Encoding.GetEncoding(httpWebResponse.ContentEncoding);

                    using(Stream sr = httpWebResponse.GetResponseStream())
                    {
                        byte[] body = new byte[httpWebResponse.ContentLength];
                        sr.Read(body, offset: 0, count: body.Length);
                        res.Body = new byte[body.Length];

                        Array.Copy(body, res.Body, body.Length);
                    }
                }

                throw new RestClientException(exWeb.Status, exWeb.Message, exWeb, res);
            }
            catch(Exception ex)
            {
                throw new RestClientException(WebExceptionStatus.UnknownError, ex.Message, ex);
            }
        }
    }
}
