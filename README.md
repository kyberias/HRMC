This is a toy C-compiler targeting Human Resource Machine (HRM) game's virtual machine implemented in C#.

The C-subset supported is parsed by an LL(1) parser and the feature set is extremely tiny:

* Only int data type supported
* Supported keywords: int, if, else, while, true, false
* Supports basic integer arithmetic (+, -, /, *, %)
* Supports basic pointer arithmetic
* Only logical and supported (&&)
* Some limitations from HRM:
	* boolean values cannot be stored into variables
	* no bit-wise operators
