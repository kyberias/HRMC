int * const Zptr = 24;
int arr[15];
int* a = *Zptr;
int one = *Zptr;
int *p = *Zptr;
int *q;
int n;

// Generate primes
++one;
n = one;
++n;
*p = n; // 2
++p;
++n;
*p = n; // 3
++p;
++n;
++n;
*p = n; // 5
++p;
++n;
++n;
*p = n; // 7
++p;
++n;
++n;
++n;
++n;
*p = n; // 11
++p;
++n;
++n;
*p = n; // 13
++p;
++n;
++n;
++n;
++n;
*p = n; // 17
++p;
++n;
++n;
*p = n; // 19
++p;
++n;
++n;
++n;
++n;
*p = n; // 23

while (true)
{
	n = input();

	while (n != one)
	{
		p = *Zptr;
		while (n % *p != 0)
		{
			++p;
		}

		output(*p);
		n = n / *p;
	}
}