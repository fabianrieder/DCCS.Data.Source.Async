# DCCS.Data.Source.Async &middot; [![Build status](https://ci.appveyor.com/api/projects/status/46hrb1mrhelwpup8?svg=true)] ![NuGet Version](https://img.shields.io/nuget/v/DCCS.Data.Source.Async.svg)

DCCS.Data.Source.Async is a fork of DCCS.Data.Source and leverages the async APIs of EntityFramework.Core

## Installation

You should install [DCCS.Data.Source.Async with NuGet](https://www.nuget.org/packages/DCCS.Data.Source/):

    Install-Package DCCS.Data.Source.Async

Or via the .NET Core command line interface:

    dotnet add package DCCS.Data.Source.Async

Either commands, from Package Manager Console or .NET Core CLI, will download and install DCCS.Data.Source.Async and all required dependencies.

## Examples

In this example we create an WebAPI action, that takes parameter (`Params`) for paging and sorting information, and returns the sorted and paged data (`Result<T>`).

```csharp
public class UsersController : Controller
{
    public AsyncResult<User> Get(Params ps)
    {
        // ...get data i.e. from EF
        // data: IQueryable<User>
        return await AsyncResult.Create(ps, data);
    }
}
```

You can also use the IQueryable extension method.

```csharp
    using DCCS.Data.Source.Async;
    ...
    // data must be an IAsyncEnumerable (as most EF Queryables are).
    return data.ToAsyncResult<User>();
```

The resulting JSON looks like this:

```javascript
{
    "data": [
        {"name": "user 1", ...},
        {"name": "user 2", ...},
        // ...
    ],
    "page": 1,
    "count": 10,
    "orderBy": "name",
    "desc": false
}
```

If you need to transform (`Select`) the sorted and paged data, you can use the `Result<T>.Select` method. Like this:

```csharp
...

using(var db = new DbContext()) {
    var users = db.Users;
    return AsyncResult.Create(ps, users).Select(user => new UserDTO(user));
}
```

**Important:** Only the paged data is transformed, so you can do this in combination with EF and it will work performantly with as many rows as your database can handle.

## Contributing

### License

DCCS.Data.Source.Async is [MIT licensed](https://opensource.org/licenses/MIT)
