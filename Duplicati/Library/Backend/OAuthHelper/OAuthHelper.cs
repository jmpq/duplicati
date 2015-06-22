﻿//  Copyright (C) 2015, The Duplicati Team
//  http://www.duplicati.com, info@duplicati.com
//
//  This library is free software; you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as
//  published by the Free Software Foundation; either version 2.1 of the
//  License, or (at your option) any later version.
//
//  This library is distributed in the hope that it will be useful, but
//  WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//  Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using System;using System.Net;using Duplicati.Library.Utility;

namespace Duplicati.Library
{
    public class OAuthHelper    {        private string m_token;        private string m_authid;        private string m_servicename;        private DateTime m_tokenExpires = DateTime.UtcNow;        public const string DUPLICATI_OAUTH_SERVICE = "https://duplicati-oauth-handler.appspot.com/refresh";        private const string OAUTH_LOGIN_URL_TEMPLATE = "https://duplicati-oauth-handler.appspot.com/type={0}";        public static string OAUTH_LOGIN_URL(string modulename) { return string.Format("https://duplicati-oauth-handler.appspot.com/type={0}", modulename); }        private static readonly string USER_AGENT = string.Format("Duplicati v{0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());        private readonly string m_user_agent;        public string OAuthLoginUrl { get; private set; }        public string UserAgent { get { return m_user_agent; } }        public OAuthHelper(string authid, string servicename, string useragent = null)        {            m_user_agent = useragent ?? USER_AGENT;            m_authid = authid;            OAuthLoginUrl = OAUTH_LOGIN_URL(servicename);            if (string.IsNullOrEmpty(authid))                throw new Exception(Strings.OAuthHelper.MissingAuthID(OAuthLoginUrl));        }        public static T GetJSONData<T>(string url, string useragent, Action<HttpWebRequest> setup = null)        {            var req = (HttpWebRequest)System.Net.WebRequest.Create(url);            req.UserAgent = useragent;            if (setup != null)                setup(req);            var areq = new AsyncHttpRequest(req);            using(var resp = (HttpWebResponse)areq.GetResponse())            using(var rs = areq.GetResponseStream())            using(var tr = new System.IO.StreamReader(rs))            using(var jr = new Newtonsoft.Json.JsonTextReader(tr))                return new Newtonsoft.Json.JsonSerializer().Deserialize<T>(jr);        }        public T GetJSONData<T>(string url, Action<HttpWebRequest> setup = null)        {            return GetJSONData<T>(url, m_user_agent, setup);        }        public T GetTokenResponse<T>(Action<HttpWebRequest> setup = null)        {            return GetJSONData<T>(DUPLICATI_OAUTH_SERVICE, m_user_agent, req =>            {                req.Headers.Add("X-AuthID", m_authid);                if (setup != null)                    setup(req);            });        }        public string AccessToken        {            get            {                if (m_token == null || m_tokenExpires < DateTime.UtcNow)                {                    try                    {                        var res = GetTokenResponse<OAuth_Service_Response>();                        m_tokenExpires = DateTime.UtcNow.AddSeconds(res.expires - 30);                        m_token = res.access_token;                    }                    catch (Exception ex)                    {                        var msg = ex.Message;                        if (ex is WebException)                        {                            var resp = ((WebException)ex).Response as HttpWebResponse;                            if (resp != null)                            {                                msg = resp.Headers["X-Reason"];                                if (string.IsNullOrWhiteSpace(msg))                                    msg = resp.StatusDescription;                            }                        }                        throw new Exception(Strings.OAuthHelper.AuthorizationFailure(msg, OAuthLoginUrl), ex);                    }                }                return m_token;            }        }        private class OAuth_Service_Response        {            public string access_token { get; set; }            [Newtonsoft.Json.JsonProperty(NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]            public int expires { get; set; }        }    }
}

