using Computers.E2e.Wire;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Computers.Tests.E2e;

public class RequestParserTests {
    [Fact]
    public void ParsesPlaceWithDefaults() {
        var request = RequestParser.Parse("place", JObject.Parse("{\"cid\":\"c1\",\"item\":\"computer\",\"x\":10,\"y\":20}"));
        var place = Assert.IsType<PlaceRequest>(request);
        Assert.Equal("computer", place.Item);
        Assert.Equal(10, place.X);
        Assert.Equal(20, place.Y);
        Assert.Equal("Farm", place.Location);
    }

    [Fact]
    public void ParsesPlaceChestItems() {
        var request = RequestParser.Parse("placeChest", JObject.Parse("{\"cid\":\"c1\",\"x\":1,\"y\":2,\"items\":[{\"itemId\":\"388\",\"count\":5}]}"));
        var chest = Assert.IsType<PlaceChestRequest>(request);
        var item = Assert.Single(chest.Items);
        Assert.Equal("388", item.ItemId);
        Assert.Equal(5, item.Count);
    }

    [Fact]
    public void ParsesDiskAndLifecycleCommands() {
        Assert.IsType<WriteDiskRequest>(RequestParser.Parse("writeDisk", JObject.Parse("{\"x\":1,\"y\":2,\"path\":\"/a.txt\",\"content\":\"hi\"}")));
        Assert.IsType<ReadDiskRequest>(RequestParser.Parse("readDisk", JObject.Parse("{\"x\":1,\"y\":2,\"path\":\"/a.txt\"}")));
        Assert.IsType<RestartComputerRequest>(RequestParser.Parse("restartComputer", JObject.Parse("{\"x\":1,\"y\":2}")));
        Assert.IsType<RemoveRequest>(RequestParser.Parse("remove", JObject.Parse("{\"x\":1,\"y\":2}")));
        Assert.IsType<InteractRequest>(RequestParser.Parse("interact", JObject.Parse("{\"x\":1,\"y\":2}")));
        Assert.IsType<QueryObjectRequest>(RequestParser.Parse("queryObject", JObject.Parse("{\"x\":1,\"y\":2}")));
    }

    [Fact]
    public void ParsesArgumentFreeCommands() {
        Assert.IsType<StatusRequest>(RequestParser.Parse("status", new JObject()));
        Assert.IsType<QueryShippingBinRequest>(RequestParser.Parse("queryShippingBin", new JObject()));
        Assert.IsType<QueryFarmhouseRequest>(RequestParser.Parse("queryFarmhouse", new JObject()));
        Assert.IsType<QueryActiveMenuRequest>(RequestParser.Parse("queryActiveMenu", new JObject()));
        Assert.IsType<StartSplitScreenRequest>(RequestParser.Parse("startSplitScreen", new JObject()));
        Assert.Equal(27, Assert.IsType<MenuKeyRequest>(RequestParser.Parse("menuKey", JObject.Parse("{\"key\":27}"))).Key);
        Assert.Equal("right", Assert.IsType<MenuClickRequest>(RequestParser.Parse("menuClick", JObject.Parse("{\"x\":1,\"y\":2,\"button\":\"right\"}"))).Button);
        Assert.IsType<InsertDiskRequest>(RequestParser.Parse("insertDisk", JObject.Parse("{\"x\":1,\"y\":2}")));
        Assert.IsType<SaveGameRequest>(RequestParser.Parse("saveGame", new JObject()));
        Assert.IsType<QuitRequest>(RequestParser.Parse("quit", new JObject()));
    }

    [Fact]
    public void ParsesWaitTicks() {
        var wait = Assert.IsType<WaitTicksRequest>(RequestParser.Parse("waitTicks", JObject.Parse("{\"count\":30}")));
        Assert.Equal(30, wait.Count);
    }

    [Fact]
    public void RejectsUnknownCommandAndMissingArgs() {
        var unknown = Assert.Throws<E2eRequestException>(() => RequestParser.Parse("nope", new JObject()));
        Assert.Equal("unknown command 'nope'", unknown.Message);
        Assert.Throws<E2eRequestException>(() => RequestParser.Parse("place", JObject.Parse("{\"x\":1,\"y\":2}")));
        Assert.Throws<E2eRequestException>(() => RequestParser.Parse("waitTicks", JObject.Parse("{\"count\":0}")));
    }
}
