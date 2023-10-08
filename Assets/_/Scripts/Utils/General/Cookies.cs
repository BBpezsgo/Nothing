using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

static class CookiesLib
{
#if UNITY_WEBGL
    [DllImport("__Internal")]
    public static extern void SetCookies(string cookies);

    [DllImport("__Internal")]
    public static extern string GetCookies();
#else
    public static void SetCookies(string cookies)
    {

    }

    public static string GetCookies()
    {
        return "";
    }
#endif
}

public static class Cookies
{
    public enum SameSite
    {
        Undefined,
        None,
        Strict,
        Lax,
    }

    static Dictionary<string, string> ToDictionary(this IEnumerable<Cookie> cookies)
    {
        Dictionary<string, string> result = new();
        foreach (Cookie item in cookies)
        { result.Add(item.Key, item.Value); }
        return result;
    }

    static void SetCookies(Cookie[] cookies)
    {
        string result = "";
        for (int i = 0; i < cookies.Length; i++)
        {
            result += cookies[i].ToString();
        }
        CookiesLib.SetCookies(result);
    }

    public struct Cookie
    {
        public readonly string Key;
        public string Value;
        public string Domain;
        public string Expires;
        public uint MaxAge;
        public bool Partitioned;
        public string Path;
        public bool Secure;
        public SameSite SameSite;

        public Cookie(string key, string value)
        {
            Key = key ?? "";
            Value = value ?? "";
            Domain = null;
            Expires = null;
            MaxAge = uint.MaxValue;
            Partitioned = false;
            Path = null;
            Secure = false;
            SameSite = SameSite.Undefined;
        }

        public Cookie(string key, string value, CookieParser.CookieClass cookieClass)
        {
            Key = key ?? "";
            Value = value ?? "";
            Domain = cookieClass.Domain;
            Expires = cookieClass.Expires;
            MaxAge = cookieClass.MaxAge;
            Partitioned = cookieClass.Partitioned;
            Path = cookieClass.Path;
            Secure = cookieClass.Secure;
            SameSite = cookieClass.SameSite;
        }

        public static implicit operator Cookie((string Key, string Value) v)
            => new(v.Key ?? "", v.Value ?? "");

        public static implicit operator (string key, string value)(Cookie v)
            => (v.Key ?? "", v.Value ?? "");

        public static implicit operator Cookie(KeyValuePair<string, string> v)
            => new(v.Key ?? "", v.Value ?? "");

        public static implicit operator KeyValuePair<string, string>(Cookie v)
            => new(v.Key ?? "", v.Value ?? "");

        public override string ToString()
        {
            string result = $"{Uri.EscapeUriString(Key)}={Uri.EscapeUriString(Value)};";

            if (!string.IsNullOrWhiteSpace(Domain))
            { result += $" domain={Domain};"; }

            if (!string.IsNullOrWhiteSpace(Expires))
            { result += $" expires={Expires};"; }

            if (MaxAge != uint.MaxValue)
            { result += $" max-age={MaxAge};"; }

            if (Partitioned)
            { result += $" partitioned;"; }

            if (!string.IsNullOrWhiteSpace(Path))
            { result += $" path={Path};"; }

            if (SameSite != SameSite.Undefined)
            { result += $" samesite={SameSite.ToString().ToLowerInvariant()};"; }

            return result;
        }
    }

    public static void SetCookie(string key, string value)
        => SetCookie(new Cookie(key, value));

    public static void SetCookie(Cookie cookie)
    {
        Dictionary<string, string> cookies = GetCookies().ToDictionary();

        if (cookies.ContainsKey(cookie.Key))
        { cookies[cookie.Key] = cookie.Value; }
        else
        { cookies.Add(cookie.Key, cookie.Value); }

        Cookie[] convertedCookies = cookies.Select(v => (Cookie)v).ToArray();
        SetCookies(convertedCookies);
    }

    public static string GetCookie(string key)
    {
        var cookies = GetCookies();
        for (int i = 0; i < cookies.Length; i++)
        {
            if (cookies[i].Key == key)
            { return cookies[i].Value; }
        }
        return null;
    }

    public static bool TryGetCookie(string key, out string value)
    {
        value = GetCookie(key);
        return value != null;
    }

    public static void ClearCookies() => CookiesLib.SetCookies("");

    public static Cookie[] GetCookies()
    {
        string data = CookiesLib.GetCookies();
        Cookie[] result = CookieParser.Parse(data);
        return result;
    }

    public class CookieParser
    {
        public class CookieClass
        {
            public string Key;
            public string Value;
            public string Domain;
            public string Expires;
            public uint MaxAge;
            public bool Partitioned;
            public string Path;
            public bool Secure;
            public SameSite SameSite;

            public CookieClass()
            {
                Key = "";
                Value = "";
                Domain = null;
                Expires = null;
                MaxAge = uint.MaxValue;
                Partitioned = false;
                Path = null;
                Secure = false;
                SameSite = SameSite.Undefined;
            }

            public Cookie ToCookie()
            {
                return new Cookie(Key, Value);
            }
        }

        public static Cookie[] Parse(string data)
        {
            string[] pairs = data.Split(';');

            CookieClass currentCookie = null;
            List<Cookie> result = new();

            for (int i = 0; i < pairs.Length; i++)
            {
                string pair = pairs[i];

                string key;
                string value = null;

                if (!pair.Contains('='))
                {
                    key = pair.Trim();
                }
                else
                {
                    key = pair.Split('=')[0].Trim();
                    value = pair.Split('=')[1].Trim();
                }

                switch (key)
                {
                    case "domain":
                        {
                            if (currentCookie == null)
                            { UnityEngine.Debug.LogWarning($"Unexpected cookie key \"{key}\""); }
                            else if (value == null)
                            { UnityEngine.Debug.LogWarning($"Cookie value for key \"{key}\" not specified"); }
                            else
                            {
                                currentCookie.Domain = value;
                            }
                            break;
                        }
                    case "expires":
                        {
                            if (currentCookie == null)
                            { UnityEngine.Debug.LogWarning($"Unexpected cookie key \"{key}\""); }
                            else if (value == null)
                            { UnityEngine.Debug.LogWarning($"Cookie value for key \"{key}\" not specified"); }
                            else
                            {
                                currentCookie.Expires = value;
                            }
                            break;
                        }
                    case "max-age":
                        {
                            if (currentCookie == null)
                            { UnityEngine.Debug.LogWarning($"Unexpected cookie key \"{key}\""); }
                            else if (value == null)
                            { UnityEngine.Debug.LogWarning($"Cookie value for key \"{key}\" not specified"); }
                            else if (!uint.TryParse(value, out uint _value))
                            { UnityEngine.Debug.LogWarning($"Invalid cookie value for key \"{key}\": \"{value}\""); }
                            else
                            {
                                currentCookie.MaxAge = _value;
                            }
                            break;
                        }
                    case "partitioned":
                        {
                            if (currentCookie == null)
                            { UnityEngine.Debug.LogWarning($"Unexpected cookie key \"{key}\""); }
                            else
                            {
                                currentCookie.Partitioned = true;
                            }
                            break;
                        }
                    case "path":
                        {
                            if (currentCookie == null)
                            { UnityEngine.Debug.LogWarning($"Unexpected cookie key \"{key}\""); }
                            else if (value == null)
                            { UnityEngine.Debug.LogWarning($"Cookie value for key \"{key}\" not specified"); }
                            else
                            {
                                currentCookie.Path = value;
                            }
                            break;
                        }
                    case "samesite":
                        {
                            if (currentCookie == null)
                            { UnityEngine.Debug.LogWarning($"Unexpected cookie key \"{key}\""); }
                            else if (value == null)
                            { UnityEngine.Debug.LogWarning($"Cookie value for key \"{key}\" not specified"); }
                            else
                            {
                                switch (value)
                                {
                                    case "lax":
                                        currentCookie.SameSite = SameSite.Lax;
                                        break;
                                    case "strict":
                                        currentCookie.SameSite = SameSite.Strict;
                                        break;
                                    case "none":
                                        currentCookie.SameSite = SameSite.None;
                                        break;
                                    default:
                                        UnityEngine.Debug.LogWarning($"Invalid cookie value for key \"{key}\": \"{value}\"");
                                        break;
                                }
                            }
                            break;
                        }
                    case "secure":
                        {
                            if (currentCookie == null)
                            { UnityEngine.Debug.LogWarning($"Unexpected cookie key \"{key}\""); }
                            else
                            {
                                currentCookie.Secure = true;
                            }
                            break;
                        }
                    default:
                        {
                            if (currentCookie == null)
                            {
                                currentCookie = new CookieClass()
                                {
                                    Key = key,
                                    Value = value,
                                };
                            }
                            else
                            {
                                result.Add(currentCookie.ToCookie());
                                currentCookie = null;
                            }
                            break;
                        }
                }
            }

            return result.ToArray();
        }
    }
}