# TickInput structure

The input for an given world tick of on specfic player (server-sided)

```csharp
public struct TickInput
```

## Public Members

| name | description |
| --- | --- |
| [Inputs](TickInput/Inputs.md) | The input |
| [PlayerId](TickInput/PlayerId.md) | The player id |
| [RemoteViewTick](TickInput/RemoteViewTick.md) | The remote world tick the player saw other entities at for this input. (This is equivalent to lastServerWorldTick on the client). |
| [WorldTick](TickInput/WorldTick.md) | The tick of the input |

## See Also

* namespace [Framework.Input](../Framework.md)
* [TickInput.cs](https://github.com/TeamStriked/godot4-fast-paced-network-fps-tps/blob/master/Example/Framework/Input/TickInput.cs)

<!-- DO NOT EDIT: generated by xmldocmd for Framework.dll -->
