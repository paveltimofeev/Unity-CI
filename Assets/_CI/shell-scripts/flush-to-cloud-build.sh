BRANCH=$(git symbolic-ref -q HEAD)
BRANCH=${BRANCH##refs/heads/}
BRANCH=${BRANCH:-HEAD}

git checkout cloud-build
git add --all 
git stash
git merge origin $BRANCH
git push

git checkout $BRANCH
