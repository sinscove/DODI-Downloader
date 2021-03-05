using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DodiDownloader
{
    public class DodiCache
    {
        private string _cachePath;
        private Dictionary<string, GameInfo> _gamePages;

        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public DodiCache()
        {
            _gamePages = new Dictionary<string, GameInfo>();
            _jsonSerializerOptions = new JsonSerializerOptions
            {
#if DEBUG
                WriteIndented = true,
#endif
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
        }

        public async Task LoadCache(string cachePath)
        {
            _cachePath = cachePath;

            try
            {
                if (File.Exists(_cachePath))
                {
                    await using FileStream cacheStream = File.OpenRead(_cachePath);
                    _gamePages =
                        await JsonSerializer.DeserializeAsync<Dictionary<string, GameInfo>>(cacheStream,
                            _jsonSerializerOptions);
                }
            }
            catch
            {
                // ignore exception and just skip loading cache
            }
        }

        public async Task<IEnumerable<string>> GetGameNames(bool regenerate)
        {
            if (regenerate || _gamePages == null || _gamePages.Count == 0)
            {
                _gamePages = await DodiScraper.GetGameList();
                await SaveCache();
            }

            return _gamePages.Keys;
        }

        public string GetDescription(string name)
        {
            if (_gamePages.TryGetValue(name, out GameInfo gameInfo))
            {
                return gameInfo.Description;
            }

            return "";
        }

        public async Task<IEnumerable<Mirror>> TryGetMirrors(string name, bool regenerate)
        {
            if (!_gamePages.TryGetValue(name, out GameInfo gameInfo))
            {
                return null;
            }

            if (regenerate || gameInfo.Mirrors == null || !gameInfo.Mirrors.Any())
            {
                gameInfo = await DodiScraper.GetGameInfo(gameInfo.PageUrl);

                _gamePages.Remove(name);
                _gamePages.Add(name, gameInfo);
                await SaveCache();
            }

            return gameInfo.Mirrors;
        }

        private async Task SaveCache()
        {
            await using FileStream cacheStream = File.Create(_cachePath, 4096, FileOptions.WriteThrough);
            await JsonSerializer.SerializeAsync(cacheStream, _gamePages, _jsonSerializerOptions);
        }
    }
}