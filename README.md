# route53
### AWS Route53 DNS management helper

usage: <code>route53 -a -D -z -l -A -T -C -h -d -v -q</code>

Make sure you have your <code>.aws/credentials</code> setup first.

Examples:

List all hosted zones (domains):
	<code>route53 -z</code>

List all entries for a domain:
	<code>route53 -d domain.com -l</code>

List all entries for a domain without details:
	<code>route53 -d domain.com -l -q</code>

Create an A record entry for a domain:
	<code>route53 -d domain.com -a -A -h <hostname> -v {ip} -t {ttl (defaults to 60)}</code>

Create a CNAME record entry for a domain:
	<code>route53 -d domain.com -a -C -h <hostname> -v {cname value} -t {ttl (defaults to 60)}</code>

Delete a CNAME record entry for a domain:
	<code>route53 -d domain.com -D -h <hostname> -C -v {cname value} -t {ttl (defaults to 60)}</code>

Delete an A record record entry for a domain:
	<code>route53 -d domain.com -D -h <hostname> -A -v {ip} -t {ttl (defaults to 60)}</code>

</code>