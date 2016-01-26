#### About Libraries
Depends on what you are going to implement. You might want the binary version of the library as you don't want to recompile everytihng on each launch.
- *libwenku8* is the core lib. You are most likely going modifying this with wenku10
- *libtaotu* is the procedural spider. use the binary version if possible
- *libtranslate* will take sooo looong to compile. So use the binary instead. But generally VS will know it doesn't need to compile everytime.
- *libpenguin* - APIs inside are deemed relatively stable. So use the binary version if possible.
- *wenku8-protocol* - Please ignore this dependency. It is private and I am not allowed to disclose the source. Sorry:(
