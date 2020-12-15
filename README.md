# route53
AWS Route53 DNS management helper

usage: route53 -a -D -z -l -A -T -C -h -d -v -q

Make sure you have your .aws/credentials setup first.

Examples:
<code>
List all hosted zones (domains):
	route53 -z

List all entries for a domain:
	route53 -d domain.com -l

List all entries for a domain without details:
	route53 -d domain.com -l -q

Create an A record entry for a domain:
	route53 -d domain.com -a -A -h <hostname> -v <ip>

Create a CNAME record entry for a domain:
	route53 -d domain.com -a -C -h <hostname> -v <cname value>

Delete a CNAME record entry for a domain:
	route53 -d domain.com -D -h <hostname> -C -v <cname value>

Delete an A record record entry for a domain:
	route53 -d domain.com -D -h <hostname> -A -v <ip>

</code>