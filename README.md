


<img align="left" src="https://raw.githubusercontent.com/undergroundwires/SafeOrbit/master/docs/img/logo/logo_60x60.png"> 

# **SafeOrbit** - Protect your data and detect injections

## SafeOrbit is easy-to-use and strong security toolkit for `.NET` and `.NET CORE`.

### The nuget package  [![NuGet Status](https://img.shields.io/nuget/v/SafeOrbit.svg?style=flat)](https://www.nuget.org/packages/SafeOrbit/)

|                 Platform | .NET Std 1.6 | .NET Std 2.0 | .NET 4.0+ |
|--------------------------|--------------|--------------|-----------|
|            Full security |     ✔        |       ✔      |     ✔     |
|  Exception serialization |     ✖        |       ✔      |     ✔     |
|   Non zero bytes randoms |     ✖        |       ✔      |     ✔     |

> It must not be required to be secret, and it must be able to fall into the hands of the enemy without inconvenience.
> -[Auguste Kerckhoffs](https://en.wikipedia.org/wiki/Kerckhoffs%27s_principle)


**SafeOrbit**'s primarly focus is [**strong memory protection**](#memory-security), it can protect any data in the memory for you: you have [SafeBytes](#safebytes#) to protect binaries, as well as [SafeString](#safestring#) to protect strings, [and even more to detect memory injections](#protect-your-classes#). **SafeOrbit** provides also bunch of tools to implement strong and high performance algorithms for [encryption, hashers and random](#cryptography).

**SafeOrbit** is **well tested** as it should be for a security library. It has more than 3.000 green tests for around 3.000 lines of code (v0.1).

**SafeOrbit** is **easy to use** as it does not require you to have a big knowledge of cryptology to take advantage of higher security.

**SafeOrbit** is **performance friendly**. Services have `Safe` or `Fast` prefixes. `Fast` classes strive for both performance and security, but `Safe` classes focuses the security over performance. **For example** while [SafeEncryptor](#aes-the-ISafeEncrpytor) uses lots of iterations, salts, and IV, [FastEncryptor](#blowfish-the-IFastEncryptor) uses a faster encryption alghoritm without any key deriving function. **Furthermore** most of the classes has a way to disable its protection. They let you change/disable the security level of the protection dynamically to gain more performance.

## Want to say thanks? :beer:

Hit the :star: star :star: button

## Contribute
Feel free to contribute by joining the coding process or opening [issues](https://github.com/undergroundwires/safeOrbit/issues). [Read more on wiki](https://github.com/undergroundwires/SafeOrbit/wiki/Contribute).

## License
[This project is MIT Licensed](LICENSE).

It means that you're free to use **SafeOrbit** freely in any application, copy, and modify its code.

# Documentation

* [Visit wiki for full documentation](https://github.com/undergroundwires/SafeOrbit/wiki)
* [Check html help file from repository](./docs/Help.chm)

## Quick documentation
**For better performance**, it's **highly recommended** to start the application early in your application start with this line :
```C#
 SafeOrbitCore.Current.StartEarly();
```
Memory injection is enabled as default. It provides self security on client side applications, but on a protected server disabling the memory injection for more performance is recommended. [Read more on wiki](https://github.com/undergroundwires/SafeOrbit/wiki/Library-settings#change-security-settings).

## Memory security

### SafeString [(wiki)](https://github.com/undergroundwires/SafeOrbit/wiki/SafeBytes)
`SafeString` represents an encrypted string that's not leaked in the memory. It has more advantages over `System.Security.SecureString` because of the security design of the **SafeOrbit**.

#### SafeString vs [System.Security.SecureString](https://msdn.microsoft.com/en-us/library/system.security.securestring(v=vs.110).aspx) 

|                              | SecureString | SafeString |
|------------------------------|-------------|------------|
|  Supports multiple encodings |      ✖      |     ✔      |
|      Safely character insert |       ✖     |     ✔      |
|      Safely character remove |       ✖     |     ✔      |
|                Safely equals |       ✖     |     ✔      |
|              Safely retrieve |       ✖     |     ✔      |
|      Reveal only single char |       ✖     |     ✔      |
|         Unlimited characters |       ✖     |     ✔      |


### SafeBytes [(wiki)](https://github.com/undergroundwires/SafeOrbit/wiki/SafeBytes)
`SafeBytes` is protected sequence of bytes in memory. You can hide the data from the memory, then modify and compare them safely without revealing the bytes. [Read more on wiki](https://github.com/undergroundwires/SafeOrbit/wiki/SafeBytes).

## Detect injections

You can detect injections for the state (data in the memory), and/or code of any `.NET` class. 

Internal protection for `SafeOrbit` library be **enabled as default**. You can disable it to gain more performance [by changing SafeOrbit's security settings](https://github.com/undergroundwires/SafeOrbit/wiki/Library-settings#change-security-settings).

### SafeObject [(wiki)](https://github.com/undergroundwires/SafeOrbit/wiki/SafeObject)
An object that can detect memory injections to itself.

```C#
    var safeObject = new SafeObject<Customer>();
    //each change to the object's state or code must be using ApplyChanges
    safeObject.ApplyChanges((customer) => customer.SensitiveInfo = "I'm protected!");
    //retrieve safe data
    var safeInfo = safeObject.Object.SensitiveInfo; //returns "I'm protected!" or alerts if any injection is detected
```

### SafeContainer [(wiki)](https://github.com/undergroundwires/SafeOrbit/wiki/SafeContainer)
**`SafeContainer`** is a dependency container that detects and notifies injections to its instances. It's security mode can be changed dynamically.

### InjectionDetector [(wiki)](https://github.com/undergroundwires/SafeOrbit/wiki/InjectionDetector)
A service that's consumed by `SafeContainer` and `SafeObject`. It's the lowest level of the injection detection and alerting mechanism.

## Cryptography

### Encryption [(wiki)](https://github.com/undergroundwires/SafeOrbit/wiki/Encryption)
Supported:
 - Asynchronous encryption
 - **Aes-256** implementation with Pbkdf2, random IV and salt. Aes-256 is considered as one of the strongest encryption algorithms. It's implemented with more security layers with a very easy to use interface in **SafeOrbit**.
 - **Blowfish** is implemented with a more secure CBC mode with IV. The implementation passes the vector tests. The algorithm is considered as one of the fastest encryption algorithms.


### Hashers [(wiki)](https://github.com/undergroundwires/SafeOrbit/wiki/Hashers)
Supported :
 - **MurmurHash (Murmur32)** for better performance, it should be seeded and salted.
 - **SHA512** for higher security.

### Random [(wiki)](https://github.com/undergroundwires/SafeOrbit/wiki/Random)
> What if your OS crypto random has in any way been undermined (for example, by a nefarious government agency, or simple incompetence)?

`SafeOrbit` guarantees not to reduce the strength of your crypto random. It has the ability to improve the strength of your crypto random.
