using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
#if UNITY_WEBGL
using System.Runtime.InteropServices;
#endif

#nullable enable

static class CookiesLib
{
#if UNITY_WEBGL
    [DllImport("__Internal")]
    public static extern void SetCookies(string cookies);

    [DllImport("__Internal")]
    public static extern string GetCookies();
#else
    public static void SetCookies(string _) { }

    public static string GetCookies() => string.Empty;
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

    static void SetCookies(ReadOnlySpan<Cookie> cookies)
    {
        StringBuilder builder = new();
        for (int i = 0; i < cookies.Length; i++)
        { cookies[i].ToString(builder); }
        CookiesLib.SetCookies(builder.ToString());
    }

    public struct Cookie
    {
        public readonly string Key;
        public string Value;
        public string? Domain;
        public string? Expires;
        public uint MaxAge;
        public bool Partitioned;
        public string? Path;
        public bool Secure;
        public SameSite SameSite;

        public Cookie(string? key, string? value)
        {
            Key = key ?? string.Empty;
            Value = value ?? string.Empty;
            Domain = null;
            Expires = null;
            MaxAge = uint.MaxValue;
            Partitioned = false;
            Path = null;
            Secure = false;
            SameSite = SameSite.Undefined;
        }

        public Cookie(string? key, string? value, CookieParser.CookieClass cookieClass)
        {
            Key = key ?? string.Empty;
            Value = value ?? string.Empty;
            Domain = cookieClass.Domain;
            Expires = cookieClass.Expires;
            MaxAge = cookieClass.MaxAge;
            Partitioned = cookieClass.Partitioned;
            Path = cookieClass.Path;
            Secure = cookieClass.Secure;
            SameSite = cookieClass.SameSite;
        }

        public static implicit operator Cookie(ValueTuple<string, string> v)
            => new(v.Item1 ?? string.Empty, v.Item2 ?? string.Empty);

        public static implicit operator ValueTuple<string, string>(Cookie v)
            => (v.Key ?? string.Empty, v.Value ?? string.Empty);

        public static implicit operator Cookie(KeyValuePair<string, string> v)
            => new(v.Key ?? string.Empty, v.Value ?? string.Empty);

        public static implicit operator KeyValuePair<string, string>(Cookie v)
            => new(v.Key ?? string.Empty, v.Value ?? string.Empty);

        public override readonly string ToString()
        {
            StringBuilder builder = new();
            ToString(builder);
            return builder.ToString();
        }

        public readonly void ToString(StringBuilder builder)
        {
            builder.Append($"{Uri.EscapeUriString(Key)}={Uri.EscapeUriString(Value)};");

            if (!string.IsNullOrWhiteSpace(Domain))
            { builder.Append($" domain={Domain};"); }

            if (!string.IsNullOrWhiteSpace(Expires))
            { builder.Append($" expires={Expires};"); }

            if (MaxAge != uint.MaxValue)
            { builder.Append($" max-age={MaxAge};"); }

            if (Partitioned)
            { builder.Append(" partitioned;"); }

            if (!string.IsNullOrWhiteSpace(Path))
            { builder.Append($" path={Path};"); }

            if (SameSite != SameSite.Undefined)
            { builder.Append($" samesite={SameSite.ToString().ToLowerInvariant()};"); }
        }
    }

    public static void SetCookie(string key, string value)
        => SetCookie(new Cookie(key, value));

    public static void SetCookie(Cookie cookie)
    {
        Dictionary<string, string> cookies = GetCookies().ToDictionary();

        cookies[cookie.Key] = cookie.Value;

        Cookie[] convertedCookies = cookies.Select(v => (Cookie)v).ToArray();
        SetCookies(convertedCookies);
    }

    public static string? GetCookie(string? key)
    {
        if (key is null) return null;
        foreach (Cookie cookie in GetCookies())
        {
            if (string.Equals(cookie.Key, key))
            { return cookie.Value; }
        }
        return null;
    }

    public static bool TryGetCookie(string key, [NotNullWhen(true)] out string? value)
    {
        value = GetCookie(key);
        return value is not null;
    }

    public static void ClearCookies() => CookiesLib.SetCookies(string.Empty);

    public static IEnumerable<Cookie> GetCookies()
    {
        string data = CookiesLib.GetCookies();
        return CookieParser.Parse(data);
    }

    public static class CookieParser
    {
        public class CookieClass
        {
            public string Key;
            public string Value;
            public string? Domain;
            public string? Expires;
            public uint MaxAge;
            public bool Partitioned;
            public string? Path;
            public bool Secure;
            public SameSite SameSite;

            public CookieClass()
            {
                Key = string.Empty;
                Value = string.Empty;
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

        public static IEnumerable<Cookie> Parse(string data)
        {
            string[] pairs = data.Split(';');

            CookieClass? currentCookie = null;

            for (int i = 0; i < pairs.Length; i++)
            {
                string pair = pairs[i];

                string key;
                string? value = null;

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
                                Value = value ?? string.Empty,
                            };
                        }
                        else
                        {
                            yield return currentCookie.ToCookie();
                            currentCookie = null;
                        }
                        break;
                    }
                }
            }
        }
    }
}
