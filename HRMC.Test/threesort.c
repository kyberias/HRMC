int a;
int b;
int c;

while(true)
{
	a = input();
	b = input();
	c = input();

	if (a <= b && a <= c)
	{
		output(a);

		if (b <= c)
		{
			output(b);
			output(c);
		}
		else 
		{
			output(c);
			output(b);
		}
	}
	else if (b <= a && b <= c)
	{
		output(b);
		if (a <= c)
		{
			output(a);
			output(c);
		}
		else
		{
			output(c);
			output(a);
		}
	}
	else 
	{
		output(c);
		if (a <= b)
		{
			output(a);
			output(b);
		}
		else
		{
			output(b);
			output(a);
		}
	}
}