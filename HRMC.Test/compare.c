int ad[10];
int ab[10];

const int *Zptr = 23;
const int *Tptr = 24;

int *a = *Zptr;
int *b = *Tptr;

int *op;

while((*a = input()) != 0)
{
	++a;
}
*a = *Zptr;

while((*b = input()) != 0)
{
	++b;
}
*b = *Zptr;

a = *Zptr;
b = *Tptr;

while(*a != 0 && *b != 0 && *a == *b)
{
	++a;
	++b;
}

if(*a == 0)
{
	op = *Zptr;
}
else if(*b == 0)
{
	op = *Tptr;
}
else if(*a < *b)
{
	op = *Zptr;
}
else
{
	op = *Tptr;
}

while(*op != 0)
{
    output(*op);
    ++op;
}
