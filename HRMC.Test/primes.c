const int *Zptr = 24;
int arr[15];
int* a = *Zptr;
int one = *Zptr;
int *p = *Zptr;
int *q;
int n;

++one;
n = one;
++n;
*p = n; 
++p;
++n;
*p = n; 
++p;
++n;
++n;
*p = n;
++p;
++n;
++n;
*p = n;
++p;
++n;
++n;
++n;
++n;
*p = n;
++p;
++n;
++n;
*p = n;
++p;
++n;
++n;
++n;
++n;
*p = n;
++p;
++n;
++n;
*p = n;
++p;
++n;
++n;
++n;
++n;
*p = n;

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