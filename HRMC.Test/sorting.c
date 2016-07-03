int ad[15];

const int *Zptr = 24;
int *a;
int n;
int i;
int *b;
int t;

while (true)
{
	a = *Zptr;
	n = *Zptr;
	while ((*a = input()) != 0)
	{
		++a;
		++n;
	}

	i = *Zptr;
	a = *Zptr;

	// 3,2,1,4,5,1
	n--;

	while (n != 0)
	{
		a = *Zptr;
		++a;
		while (*a != 0)
		{
			b = a;
			--b;
			if (*a < *b)
			{
				t = *a;
				*a = *b;
				*b = t;
			}
			++a;
		}

		--n;
	}

	a = *Zptr;
	while (*a != 0)
	{
		output(*a);
		++a;
	}
}