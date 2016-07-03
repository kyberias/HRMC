// Grab each number from INBOX and send its digits to the OUTBOX. For example 123 becomes 1,2,3

const int *ZeroPtr = 9;
const int *TenPtr = 10;
const int *HundredPtr = 11;

int n;
int ones;
int tens;
int hundreds;

while (true)
{
	ones = *ZeroPtr;
	tens = *ZeroPtr;
	hundreds = *ZeroPtr;

	n = input();

	while (n - *HundredPtr >= 0)
	{
		n = n - *HundredPtr;
		++hundreds;
	}

	if (hundreds != 0)
	{
		output(hundreds);
	}

	while (n - *TenPtr >= 0)
	{
		n = n - *TenPtr;
		++tens;
	}

	if (hundreds != 0)
	{
		output(tens);
	}
	else if (tens != 0)
	{
		output(tens);
	}
	output(n);
}
