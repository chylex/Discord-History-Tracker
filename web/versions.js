// TODO for PR: Change these:
GH_REPO = "will-ca/Discord-History-Tracker";
GH_DEFAULT_BRANCH = "main-browser";

function ghUrl(file, commit = null) {
	return "https://raw.githubusercontent.com/" +
		GH_REPO + "/" +
		(commit ?? GH_DEFAULT_BRANCH) +
		file;
	// API documents CORS:
	// https://docs.github.com/en/rest/overview/resources-in-the-rest-api?apiVersion=2022-11-28#cross-origin-resource-sharing
	// Not sure a about `raw.githubusercontent`. But it's been available at least for several years:
	// https://gitlab.com/gitlab-org/gitlab/-/issues/16732
}

async function ghFetch(file) {
	let url = ghUrl(file);
	console.log(`Fetching ${file} from ${url}.`);
	let response = await fetch(url, {cache: 'reload'});
	console.log(`Fetched ${file}.`);
	// Prob not worth handling network and HTTP errors.
	return response;
}

ajax_versions = ghFetch("/VERSIONS.json").then(r=>r.json());
