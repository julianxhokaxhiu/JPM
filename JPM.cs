/*
    The MIT License (MIT)

    Copyright (c) 2013 Julian Xhokaxhiu

    Permission is hereby granted, free of charge, to any person obtaining a copy of
    this software and associated documentation files (the "Software"), to deal in
    the Software without restriction, including without limitation the rights to
    use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
    the Software, and to permit persons to whom the Software is furnished to do so,
    subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
    FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
    COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
    IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
    CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace com.julianxhokaxhiu
{

    public enum JPMStatus
    {
        NOT_FOUND,
        INSTALLED,
        UPDATED,
        REMOVED
    }

    public delegate JPMStatus JPMPackageCallback(dynamic package);

    public class JPM
    {
        private JsonFile sourceList = null;
        private JsonFile packageList = new JsonFile("packages.json");
        private JsonFile installedList = new JsonFile("installed.json");

        public JPM(string sourceFile)
        {
            if (sourceFile.Length > 0) sourceList = new JsonFile(sourceFile);
        }

        async public Task<int> Refresh()
        {
            var status = 0;

            var client = new JsonClient();
            foreach (var source in sourceList.json)
            {
                List<dynamic> ret = await client.DoRequestJsonAsync<List<dynamic>>(source);
                packageList.json = Extend(packageList.json, ret);
            }

            packageList.Save();

            return status;
        }

        public List<dynamic> GetInstalledPackages()
        {
            return installedList.json;
        }

        public List<dynamic> GetAvailablePackages()
        {
            return packageList.json;
        }

        public List<dynamic> GetUpdatedPackages(List<dynamic> present, List<dynamic> updated)
        {
            var ret = new List<dynamic>();

            foreach (var installed in installedList.json)
            {
                foreach (var package in packageList.json)
                {
                    if (package.name == installed.name)
                    {
                        Version installedVersion = new Version((string)installed.version);
                        Version packageVersion = new Version((string)package.version);
                        if (packageVersion > installedVersion) ret.Add(package);
                    }
                }
            }

            return ret;
        }

        public JPMStatus InstallPackage(int index, JPMPackageCallback callback)
        {
            JPMStatus ret = JPMStatus.NOT_FOUND;

            try
            {
                var package = packageList.json[index];
                ret = callback(package);
                switch (ret)
                {
                    case JPMStatus.INSTALLED:
                        installedList.json.Add(package);
                        break;
                    case JPMStatus.UPDATED:
                        var installed = GetItem(installedList.json, package);
                        var idx = installedList.json.IndexOf(installed);
                        installedList.json.Remove(installed);
                        installedList.json.Insert(idx, package);
                        break;
                }
            }
            catch (Exception ex)
            {
                // Nothing
            }

            return ret;
        }

        public JPMStatus RemovePackage(int index, JPMPackageCallback callback)
        {
            JPMStatus ret = JPMStatus.NOT_FOUND;

            try
            {
                var package = installedList.json[index];
                ret = callback(package);
                if (ret == JPMStatus.REMOVED) installedList.json.Remove(package);
            }
            catch (Exception ex)
            {

            }

            return ret;
        }

        #region "Utility"
        private List<dynamic> Extend(params List<dynamic>[] lists)
        {
            var ret = new List<dynamic>();

            if (lists.Length > 0)
            {
                var first = lists[0];
                foreach (var list in lists)
                {
                    if (list != first) first = Merge(first, list);
                }
                ret.AddRange(first);
            }

            return ret;
        }

        private List<dynamic> Merge(List<dynamic> dest, List<dynamic> source)
        {
            var ret = new List<dynamic>(dest);

            foreach (var obj in source)
            {
                var add = true;
                int idx = -1;
                foreach (var item in dest)
                {
                    if (item.name == obj.name)
                    {
                        add = false;

                        Version itemVersion = new Version((string) item.version);
                        Version objVersion = new Version((string) obj.version);
                        if (objVersion > itemVersion)
                        {
                            idx = ret.IndexOf(item);
                            ret.Remove(item);
                            add = true;
                        }
                    }
                }
                if (add)
                {
                    if (idx > -1) ret.Insert(idx, obj);
                    else ret.Add(obj);
                }
            }

            return ret;
        }

        private dynamic GetItem(List<dynamic> list, dynamic item)
        {
            dynamic ret = null;

            foreach (var package in list)
            {
                if (item.name == package.name) ret = package;
            }

            return ret;
        }
        #endregion

        #region "Classes"
        private class JsonClient
        {
            public async Task<System.IO.TextReader> DoRequestAsync(WebRequest req)
            {
                var task = Task.Factory.FromAsync((cb, o) => ((HttpWebRequest)o).BeginGetResponse(cb, o), res => ((HttpWebRequest)res.AsyncState).EndGetResponse(res), req);
                var result = await task;
                var resp = result;
                var stream = resp.GetResponseStream();
                var sr = new System.IO.StreamReader(stream);
                return sr;
            }

            public async Task<System.IO.TextReader> DoRequestAsync(string url)
            {
                HttpWebRequest req = HttpWebRequest.CreateHttp(url);
                var tr = await DoRequestAsync(req);
                return tr;
            }

            public async Task<T> DoRequestJsonAsync<T>(string uri)
            {
                HttpWebRequest req = HttpWebRequest.CreateHttp(uri);
                var ret = await DoRequestAsync(req);
                var response = await ret.ReadToEndAsync();
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(response);
            }
        }

        private class JsonFile
        {
            public List<dynamic> json = new List<dynamic>();
            private string path = null;

            public JsonFile(string filePath)
            {
                if (filePath.Length > 0)
                {
                    path = filePath;
                    if (System.IO.Path.GetDirectoryName(path).Length == 0) path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + path;
                    if (System.IO.File.Exists(path))
                    {
                        json = JsonConvert.DeserializeObject<List<dynamic>>(System.IO.File.ReadAllText(filePath));
                    }
                    else
                    {
                        json = new List<dynamic>();
                    }
                }
            }

            public void Save()
            {
                System.IO.File.WriteAllText(path, JsonConvert.SerializeObject(json, Formatting.Indented));
            }
        }
        #endregion
    }
}
