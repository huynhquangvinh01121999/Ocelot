.. _Program: https://github.com/ThreeMammals/Ocelot/blob/main/samples/Basic/Program.cs

Caching
=======

Ocelot supports some very rudimentary caching at the moment provider by the `CacheManager <https://github.com/MichaCo/CacheManager>`_ project.
This is an amazing project that is solving a lot of caching problems. We would recommend using this package to cache with Ocelot. 

The following example shows how to add *CacheManager* to Ocelot so that you can do output caching. 

Install
-------

First of all, add the following `Ocelot.Cache.CacheManager <https://www.nuget.org/packages/Ocelot.Cache.CacheManager>`_ package:

.. code-block:: powershell

    Install-Package Ocelot.Cache.CacheManager

This will give you access to the Ocelot cache manager extension methods.
The second step is to add the following to your `Program`_:

.. code-block:: csharp

    using Ocelot.Cache.CacheManager;

    builder.Services
        .AddOcelot(builder.Configuration)
        .AddCacheManager(x => x.WithDictionaryHandle());

Configuration
-------------

Finally, in order to use caching on a route in your route configuration add this setting:

.. code-block:: json

    "FileCacheOptions": {
      "TtlSeconds": 15,
      "Region": "europe-central",
      "Header": "OC-Caching-Control",
      "EnableContentHashing": false // my route has GET verb only, assigning 'true' for requests with body: POST, PUT etc.
    }

* In this example ``TtlSeconds`` is set to 15 which means the cache will expire after 15 seconds.
* The ``Region`` represents a region of caching.
  The cache for a region can be cleared by calling Ocelot's :ref:`administration-api`.
* If a header name is defined in the ``Header`` property, that header value is looked up by the key (header name) in the ``HttpRequest`` headers, and if the header is found, its value will be included in caching key.
  This causes the cache to become invalid due to the header value changing.

.. _caching-enablecontenthashing-option:

``EnableContentHashing`` option
-------------------------------

In version `23.0`_, the new property ``EnableContentHashing`` has been introduced.
Previously, the request body was utilized to compute the cache key.
However, due to potential performance issues arising from request body hashing, it has been disabled by default.
Clearly, this constitutes a breaking change and presents challenges for users who require cache key calculations that consider the request body (e.g., for the POST method).
To address this issue, it is recommended to enable the option either at the route level or globally in the :ref:`caching-global-configuration` section:

.. code-block:: json

    "CacheOptions": {
      // ...
      "EnableContentHashing": true
    }

.. _caching-global-configuration:

Global Configuration
--------------------

The positive update is that copying route-level properties for each route is no longer necessary, as version `23.3`_ allows for setting their values in the ``GlobalConfiguration``.
This convenience extends to ``Header`` and ``Region`` as well.
However, an alternative is still being sought for ``TtlSeconds``, which must be explicitly set for each route to enable caching.

.. Sample
.. -----

.. If you look at the example `here <https://github.com/ThreeMammals/Ocelot/blob/main/test/Ocelot.ManualTest/Program.cs>`_ you can see how the cache manager is setup and then passed into the Ocelot ``AddCacheManager`` configuration method.
.. You can use any settings supported by the **CacheManager** package and just pass them in.

Notes
-----

Ocelot currently supports caching on the URL of the downstream service and setting a TTL in seconds to expire the cache.
You can also clear the cache for a region by calling Ocelot's :ref:`administration-api`.

Your Own Caching
----------------

If you want to add your own caching method, implement the following interfaces and register them in DI e.g.

.. code-block:: csharp

    builder.Services
        .AddSingleton<IOcelotCache<CachedResponse>, MyCache>();

* ``IOcelotCache<CachedResponse>`` this is for output caching.
* ``IOcelotCache<FileConfiguration>`` this is for caching the file configuration if you are calling something remote to get your config such as Consul.

Future
------

Please dig into the Ocelot source code to find more.
We would really appreciate it if anyone wants to implement `Redis <https://redis.io/>`_, `Memcached <http://www.memcached.org/>`_ etc.
Please, open a new `Show and tell <https://github.com/ThreeMammals/Ocelot/discussions/categories/show-and-tell>`_ thread in `Discussions <https://github.com/ThreeMammals/Ocelot/discussions>`_ space of the repository.

.. _23.0: https://github.com/ThreeMammals/Ocelot/releases/tag/23.0.0
.. _23.3: https://github.com/ThreeMammals/Ocelot/releases/tag/23.3.0
