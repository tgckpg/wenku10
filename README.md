# What is wenku10
wenku10 versatile book reader that does many things.
#### Current features
- Provide OneDrive syncing for bookmarks
- Auto bookmarks and custom bookmarks feature
- A spider that can crawl web content into a book
- Themes
- ...actually it is just a text reader that I created for reading web novel
- way to go!

#### Going to implement
- An embed dictionary ( I need this for learning Japanese, so ... whatever I want! )
- Faster vertical text rendering ( I am planning to switch the core components to native library )
- Better design. I don't think if I have time to do this

#### How to compile
- Sign the assembly first, for all project, follow the steps below:
  1. Right click project properties -> Signing -> Uncheck signing
  2. Under project -> expand the [Properties] group -> Edit AssemblyInfo.cs
  3. Remove the `, PublicKey...` suffix inside `IntenalsVisibleTo`
  4. Compile the project
  5. Once the project is compile and the dll is present. Enable the signing again
  6. Generate the publickey and put it after `InternalsVisibleTo`
- For `libtranslate`, you need to generate the code before compiling

#### Screenshots
Here are some screenshots as of version 1.2.3b
![Screenshot 1](https://tgckpg.github.io/wenku10/screenshots/zh-tw (1).png)
![Screenshot 2](https://tgckpg.github.io/wenku10/screenshots/zh-tw (2).png)
![Screenshot 3](https://tgckpg.github.io/wenku10/screenshots/zh-tw (3).png)
![Screenshot 4](https://tgckpg.github.io/wenku10/screenshots/zh-tw (4).png)
![Screenshot 5](https://tgckpg.github.io/wenku10/screenshots/zh-tw (5).png)
