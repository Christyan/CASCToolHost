﻿using CASCToolHost.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CASCToolHost.Controllers
{
    [Route("casc/install")]
    [ApiController]
    public class InstallController : Controller
    {
        [Route("dumpbybuild")]
        public ActionResult DumpByBuild(string buildConfig)
        {
            var build = BuildCache.GetOrCreate(buildConfig);

            string installHash;

            if(build.buildConfig.install.Length == 2)
            {
                installHash = build.buildConfig.install[1].ToHexString().ToLower();
            }
            else
            {
                if (build.encoding.aEntries.TryGetValue(build.buildConfig.install[0], out var installEntry))
                {
                    installHash = installEntry.eKey.ToHexString().ToLower();
                }
                else
                {
                    throw new KeyNotFoundException("Root encoding key not found!");
                }
            }

            return DumpByHash(installHash);
        }

        [Route("dump")]
        public ActionResult DumpByHash(string hash)
        {
            var install = NGDP.GetInstall("http://cdn.blizzard.com/tpr/wow/", hash, true);
            return Json(install.entries);
        }

        [Route("diff")]
        public ActionResult Diff(string from, string to)
        {
            var installFrom = NGDP.GetInstall("http://cdn.blizzard.com/tpr/wow/", from, true);
            var installTo = NGDP.GetInstall("http://cdn.blizzard.com/tpr/wow/", to, true);

            var installFromDict = new Dictionary<string, InstallFileEntry>();
            foreach(var entry in installFrom.entries)
            {
                installFromDict.Add(entry.name, entry);
            }

            var installToDict = new Dictionary<string, InstallFileEntry>();
            foreach (var entry in installTo.entries)
            {
                installToDict.Add(entry.name, entry);
            }

            var fromEntries = installFromDict.Keys.ToHashSet();
            var toEntries = installToDict.Keys.ToHashSet();

            var commonEntries = fromEntries.Intersect(toEntries);
            var removedEntries = fromEntries.Except(commonEntries);
            var addedEntries = toEntries.Except(commonEntries);

            var modifiedFiles = new List<InstallFileEntry>();
            foreach (var entry in commonEntries)
            {
                var originalFile = installFromDict[entry];
                var patchedFile = installToDict[entry];

                if (originalFile.contentHash.Equals(patchedFile.contentHash))
                {
                    continue;
                }

                modifiedFiles.Add(patchedFile);
            }

            return Json(new
            {
                added = addedEntries,
                modified = modifiedFiles,
                removed = removedEntries
            });
        }
    }
}