using System.Collections;
using System.Collections.Generic;
using Gulde.Cities;
using Gulde.Company.Employees;
using Gulde.Economy;
using Gulde.Maps;
using Gulde.Timing;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Gulde.Builders
{
    public class CityBuilder : Builder
    {
        public GameObject CityObject { get; private set; }

        [LoadAsset("prefab_city")]
        GameObject CityPrefab { get; set; }

        [LoadAsset("prefab_market")]
        GameObject MarketPrefab { get; set; }

        Vector2Int MapSize { get; set; }
        int WorkerHomeCount { get; set; }
        Vector3Int MarketPosition { get; set; }
        HashSet<Vector3Int> WorkerHomePositions { get; } = new HashSet<Vector3Int>();
        List<CompanyBuilder> CompaniesToBuild { get; } = new List<CompanyBuilder>();
        int Hour { get; set; }
        int Minute { get; set; }
        int Year { get; set; }

        public CityBuilder() : base()
        {

        }

        public CityBuilder WithSize(int x, int y)
        {
            MapSize = new Vector2Int(x, y);
            return this;
        }

        public CityBuilder WithWorkerHomes(int count)
        {
            WorkerHomeCount = count;
            return this;
        }

        public CityBuilder WithWorkerHome(int x, int y)
        {
            WorkerHomePositions.Add(new Vector3Int(x, y, 0));
            return this;
        }

        public CityBuilder WithMarket(int x, int y)
        {
            MarketPosition = new Vector3Int(x, y, 0);
            return this;
        }

        public CityBuilder WithCompany(CompanyBuilder companyBuilder)
        {
            CompaniesToBuild.Add(companyBuilder);
            return this;
        }

        public CityBuilder WithTime(int hour, int minute, int year)
        {
            Hour = hour;
            Minute = minute;
            Year = year;
            return this;
        }

        public override IEnumerator Build()
        {
            yield return base.Build();

            CityObject = Object.Instantiate(CityPrefab);

            var city = CityObject.GetComponent<CityComponent>();
            var map = CityObject.GetComponent<MapComponent>();
            var time = CityObject.GetComponent<TimeComponent>();

            time.Hour = Hour;
            time.Minute = Minute;
            time.Year = Year;

            var workerHomeBuilder = new WorkerHomeBuilder().WithMap(map);

            for (var i = 0; i < WorkerHomeCount; i++)
            {
                var x = Random.Range(-map.Size.x / 2, map.Size.x / 2);
                var y = Random.Range(-map.Size.y / 2, map.Size.y / 2);

                yield return workerHomeBuilder.WithEntryCell(x, y).Build();
            }

            foreach (var cell in WorkerHomePositions)
            {
                yield return workerHomeBuilder.WithEntryCell(cell).Build();
            }

            var marketObject = Object.Instantiate(MarketPrefab, CityObject.transform);
            var market = marketObject.GetComponent<MarketComponent>();
            market.Location.EntryCell = MarketPosition;

            foreach (var companyBuilder in CompaniesToBuild)
            {
                yield return companyBuilder.WithMap(map).Build();
            }
        }
    }
}